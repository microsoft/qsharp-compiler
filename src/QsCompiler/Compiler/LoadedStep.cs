// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler
{
    /// <summary>
    /// Concrete implementation of a rewrite steps with an additional property specifying the dll it was loaded from.
    /// </summary>
    internal class LoadedStep : IRewriteStep
    {
        internal readonly Uri Origin;
        private readonly IRewriteStep? selfAsStep;
        private readonly object selfAsObject;

        private readonly MethodInfo[]? interfaceMethods;

        private MethodInfo? InterfaceMethod(string name) =>
            // This choice of filtering the interface methods may seem a bit particular.
            // However, unless you know what you are doing, please don't change it.
            // If you are sure you know what you are doing, please make sure the loading via reflection works for rewrite steps
            // implemented in both F# or C#, and whether they are compiled against the current compiler version or an older one.
            this.interfaceMethods?.FirstOrDefault(method => method.Name.Split("-").Last() == name);

        private object? GetViaReflection(string name) =>
            this.InterfaceMethod($"get_{name}")?.Invoke(this.selfAsObject, null);

        [return: MaybeNull]
        private T GetViaReflection<T>(string name) =>
            this.InterfaceMethod($"get_{name}")?.Invoke(this.selfAsObject, null) is T result ? result : default;

        private void SetViaReflection<T>(string name, T arg) =>
            this.InterfaceMethod($"set_{name}")?.Invoke(this.selfAsObject, new object?[] { arg });

        [return: MaybeNull]
        private T InvokeViaReflection<T>(string name, params object?[] args) =>
            this.InterfaceMethod(name)?.Invoke(this.selfAsObject, args) is T result ? result : default;

        private LoadedStep()
        {
            this.Origin = new Uri(this.GetType().Assembly.Location);
            this.selfAsObject = new object();
            this.Name = "Empty";
        }

        internal LoadedStep(IRewriteStep rewriteStep, string? outputFolder = null)
        {
            this.selfAsObject = rewriteStep;
            this.Origin = new Uri(rewriteStep.GetType().Assembly.Location);
            this.OutputFolder = outputFolder;
            this.selfAsStep = rewriteStep;
            this.Name = this.selfAsStep.Name;
            this.Priority = this.selfAsStep.Priority;
        }

        /// <summary>
        /// Attempts to construct a rewrite step via reflection.
        /// Note that the loading via reflection has the consequence that methods may fail on execution.
        /// This is e.g. the case if they invoke methods from package references if the corresponding dll
        /// has not been copied to output folder of the dll from which the rewrite step is loaded.
        /// Throws the corresponding exception if that construction fails.
        /// </summary>
        internal LoadedStep(object implementation, Type interfaceType, Uri origin, string? outputFolder = null)
        {
            this.Origin = origin;
            this.OutputFolder = outputFolder;
            this.selfAsObject = implementation;

            // Initializing the _InterfaceMethods even if the implementation implements IRewriteStep
            // would result in certain properties being loaded via reflection instead of simply being accessed via _SelfAsStep.
            if (this.selfAsObject is IRewriteStep step)
            {
                this.selfAsStep = step;
            }
            else
            {
                this.interfaceMethods = implementation.GetType().GetInterfaceMap(interfaceType).TargetMethods;
            }

            // The Name and Priority need to be fixed throughout the loading,
            // so whatever their value is when loaded that's what these values well be as far at the compiler is concerned.
            this.Name = this.selfAsStep?.Name
                ?? this.GetViaReflection<string>(nameof(IRewriteStep.Name))
                ?? "(no name)";
            this.Priority = this.selfAsStep?.Priority ?? this.GetViaReflection<int>(nameof(IRewriteStep.Priority));
        }

        public string Name { get; }

        public int Priority { get; }

        public string? OutputFolder { get; }

        internal static Diagnostic ConvertDiagnostic(IRewriteStep.Diagnostic diagnostic, Func<DiagnosticSeverity, string?>? getCode = null)
        {
            var severity =
                diagnostic.Severity == CodeAnalysis.DiagnosticSeverity.Error ? DiagnosticSeverity.Error :
                diagnostic.Severity == CodeAnalysis.DiagnosticSeverity.Warning ? DiagnosticSeverity.Warning :
                diagnostic.Severity == CodeAnalysis.DiagnosticSeverity.Info ? DiagnosticSeverity.Information :
                DiagnosticSeverity.Hint;

            var stageAnnotation =
                diagnostic.Stage == IRewriteStep.Stage.PreconditionVerification ? $"[{diagnostic.Stage}] " :
                diagnostic.Stage == IRewriteStep.Stage.PostconditionVerification ? $"[{diagnostic.Stage}] " :
                "";

            // NOTE: If we change data structure to add or change properties,
            // then the cast below in GeneratedDiagnostics needs to be adapted.
            return new Diagnostic
            {
                Code = getCode?.Invoke(severity),
                Severity = severity,
                Message = $"{stageAnnotation}{diagnostic.Message}",
                Source = diagnostic.Source,
                Range = diagnostic.Source is null ? null : diagnostic.Range?.ToLsp()
            };
        }

        public IDictionary<string, string?> AssemblyConstants
        {
            get => this.selfAsStep?.AssemblyConstants
                ?? this.GetViaReflection<IDictionary<string, string?>>(nameof(IRewriteStep.AssemblyConstants))
                ?? ImmutableDictionary<string, string?>.Empty;
        }

        public IEnumerable<IRewriteStep.Diagnostic> GeneratedDiagnostics
        {
            get
            {
                if (this.selfAsStep != null)
                {
                    return this.selfAsStep.GeneratedDiagnostics;
                }
                static bool IEnumerableInterface(Type t) => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                var enumerable = this.GetViaReflection(nameof(IRewriteStep.GeneratedDiagnostics)) as IEnumerable;
                var itemType = enumerable?.GetType().GetInterfaces().FirstOrDefault(IEnumerableInterface)?.GetGenericArguments().FirstOrDefault();
                if (enumerable is null || itemType is null)
                {
                    return Enumerable.Empty<IRewriteStep.Diagnostic>();
                }

                var diagnostics = ImmutableArray.CreateBuilder<IRewriteStep.Diagnostic>();
                foreach (var obj in enumerable)
                {
                    if (obj == null)
                    {
                        continue;
                    }
                    diagnostics.Add(new IRewriteStep.Diagnostic
                    {
                        Severity = (CodeAnalysis.DiagnosticSeverity)itemType.GetProperty(nameof(IRewriteStep.Diagnostic.Severity)).GetValue(obj, null),
                        Message = itemType.GetProperty(nameof(IRewriteStep.Diagnostic.Message)).GetValue(obj, null) as string,
                        Source = itemType.GetProperty(nameof(IRewriteStep.Diagnostic.Source)).GetValue(obj, null) as string,
                        Range = itemType.GetProperty(nameof(IRewriteStep.Diagnostic.Range)).GetValue(obj, null) as Range
                    });
                }
                return diagnostics.ToImmutable();
            }
        }

        public bool ImplementsTransformation
        {
            get => this.selfAsStep?.ImplementsTransformation
                ?? this.GetViaReflection<bool>(nameof(IRewriteStep.ImplementsTransformation));
        }

        public bool ImplementsPreconditionVerification
        {
            get => this.selfAsStep?.ImplementsPreconditionVerification
                ?? this.GetViaReflection<bool>(nameof(IRewriteStep.ImplementsPreconditionVerification));
        }

        public bool ImplementsPostconditionVerification
        {
            get => this.selfAsStep?.ImplementsPostconditionVerification
                ?? this.GetViaReflection<bool>(nameof(IRewriteStep.ImplementsPostconditionVerification));
        }

        public bool Transformation(QsCompilation compilation, [NotNullWhen(true)] out QsCompilation? transformed)
        {
            if (this.selfAsStep != null)
            {
                return this.selfAsStep.Transformation(compilation, out transformed);
            }
            var args = new object?[] { compilation, null };
            var success = this.InvokeViaReflection<bool>(nameof(IRewriteStep.Transformation), args);
            transformed = success ? args[1] as QsCompilation ?? compilation : compilation;
            return success;
        }

        public bool PreconditionVerification(QsCompilation compilation) =>
            this.selfAsStep?.PreconditionVerification(compilation)
            ?? this.InvokeViaReflection<bool>(nameof(IRewriteStep.PreconditionVerification), compilation);

        public bool PostconditionVerification(QsCompilation? compilation) =>
            !(compilation is null)
            && (this.selfAsStep?.PostconditionVerification(compilation)
                ?? this.InvokeViaReflection<bool>(nameof(IRewriteStep.PostconditionVerification), compilation));
    }
}
