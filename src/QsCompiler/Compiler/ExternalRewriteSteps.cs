// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler
{
    internal static class RewriteSteps
    {
        /// <summary>
        /// Concrete implementation of a rewrite steps with an additional property specifying the dll it was loaded from. 
        /// </summary>
        internal class LoadedStep : IRewriteStep
        {
            internal readonly Uri Origin;
            private readonly IRewriteStep _SelfAsStep;
            private readonly object _SelfAsObject;

            private readonly MethodInfo[] _InterfaceMethods;
            private MethodInfo InterfaceMethod(string name) =>
                // This choice of filtering the interface methods may seem a bit particular. 
                // However, unless you know what you are doing, please don't change it. 
                // If you are sure you know what you are doing, please make sure the loading via reflection works for rewrite steps 
                // implemented in both F# or C#, and whether they are compiled against the current compiler version or an older one.
                this._InterfaceMethods?.FirstOrDefault(method => method.Name.Split("-").Last() == name);

            private T GetViaReflection<T>(string name) =>
                (T)InterfaceMethod($"get_{name}")?.Invoke(_SelfAsObject, null);

            private void SetViaReflection<T>(string name, T arg) =>
                InterfaceMethod($"set_{name}")?.Invoke(_SelfAsObject, new object[] { arg });

            private T InvokeViaReflection<T>(string name, params object[] args) => 
                (T)InterfaceMethod(name)?.Invoke(_SelfAsObject, args);


            /// <summary>
            /// Attempts to construct a rewrite step via reflection.
            /// Note that the loading via reflection has the consequence that methods may fail on execution. 
            /// This is e.g. the case if they invoke methods from package references if the corresponding dll 
            /// has not been copied to output folder of the dll from which the rewrite step is loaded. 
            /// Throws the corresponding exception if that construction fails. 
            /// </summary>
            internal LoadedStep(object implementation, Type interfaceType, Uri origin)
            {
                this.Origin = origin ?? throw new ArgumentNullException(nameof(origin));
                this._SelfAsObject = implementation ?? throw new ArgumentNullException(nameof(implementation));

                // Initializing the _InterfaceMethods even if the implementation implements IRewriteStep 
                // would result in certain properties being loaded via reflection instead of simply being accessed via _SelfAsStep.
                if (this._SelfAsObject is IRewriteStep step) this._SelfAsStep = step;
                else this._InterfaceMethods = implementation.GetType().GetInterfaceMap(interfaceType).TargetMethods;

                // The Name and Priority need to be fixed throughout the loading, 
                // so whatever their value is when loaded that's what these values well be as far at the compiler is concerned.
                this.Name = _SelfAsStep?.Name ?? this.GetViaReflection<string>(nameof(IRewriteStep.Name)); 
                this.Priority = _SelfAsStep?.Priority ?? this.GetViaReflection<int>(nameof(IRewriteStep.Priority)); 
            }

            public string Name { get; }
            public int Priority { get; }
            public IDictionary<string, string> AssemblyConstants 
            {
                get => _SelfAsStep?.AssemblyConstants 
                    ?? this.GetViaReflection<IDictionary<string, string>>(nameof(IRewriteStep.AssemblyConstants));
            }

            public bool ImplementsTransformation
            {
                get => _SelfAsStep?.ImplementsTransformation 
                    ?? this.GetViaReflection<bool>(nameof(IRewriteStep.ImplementsTransformation));
            }

            public bool ImplementsPreconditionVerification
            {
                get => _SelfAsStep?.ImplementsPreconditionVerification 
                    ?? this.GetViaReflection<bool>(nameof(IRewriteStep.ImplementsPreconditionVerification));
            }

            public bool ImplementsPostconditionVerification
            {
                get => _SelfAsStep?.ImplementsPostconditionVerification 
                    ?? this.GetViaReflection<bool>(nameof(IRewriteStep.ImplementsPostconditionVerification));
            }

            public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
            {
                if (_SelfAsStep != null) return _SelfAsStep.Transformation(compilation, out transformed);
                var args = new object[] { compilation, null };
                var success = this.InvokeViaReflection<bool>(nameof(IRewriteStep.Transformation), args);
                transformed = success ? (QsCompilation)args[1] : compilation;
                return success;
            }

            public bool PreconditionVerification(QsCompilation compilation) =>
                _SelfAsStep?.PreconditionVerification(compilation)
                ?? this.InvokeViaReflection<bool>(nameof(IRewriteStep.PreconditionVerification), compilation);

            public bool PostconditionVerification(QsCompilation compilation) =>
                _SelfAsStep?.PostconditionVerification(compilation)
                ?? this.InvokeViaReflection<bool>(nameof(IRewriteStep.PostconditionVerification), compilation);
        }


        /// <summary>
        /// Loads all dlls listed as containing rewrite steps to include in the compilation process in the given configuration.
        /// Generates suitable diagnostics if a listed file can't be found or loaded. 
        /// Finds all types implementing the IRewriteStep interface and loads the corresponding rewrite steps
        /// according to the specified priority, where a steps with a higher priority will be listed first in the returned array. 
        /// If the function onDiagnostic is specified and not null, calls it on all generated diagnostics, 
        /// and calls onException on all caught exceptions if it is specified and not null. 
        /// Returns an empty array if the rewrite steps in the given configurations are set to null. 
        /// </summary>
        internal static ImmutableArray<LoadedStep> Load(CompilationLoader.Configuration config,
            Action<Diagnostic> onDiagnostic = null, Action<Exception> onException = null)
        {
            if (config.RewriteSteps == null) return ImmutableArray<LoadedStep>.Empty;
            static Assembly LoadAssembly(string path) => CompilationLoader.LoadAssembly?.Invoke(path) ?? Assembly.LoadFrom(path);
            Uri WithFullPath(string file)
            {
                try
                {
                    return String.IsNullOrWhiteSpace(file) ? null : new Uri(Path.GetFullPath(file));
                }
                catch (Exception ex)
                {
                    onDiagnostic?.Invoke(Errors.LoadError(ErrorCode.InvalidFilePath, new[] { file }, file));
                    onException?.Invoke(ex);
                    return null;
                }
            }

            var specifiedPluginDlls = config.RewriteSteps.Select(step => (WithFullPath(step.Item1), step.Item2)).Where(step => step.Item1 != null).ToList();
            var (foundDlls, notFoundDlls) = specifiedPluginDlls.Partition(step => File.Exists(step.Item1.LocalPath));
            foreach (var file in notFoundDlls.Select(step => step.Item1).Distinct())
            {
                onDiagnostic?.Invoke(Errors.LoadError(ErrorCode.UnknownCompilerPlugin, new[] { file.LocalPath }, file.LocalPath));
            }

            var rewriteSteps = ImmutableArray.CreateBuilder<LoadedStep>();
            foreach (var (target, outputFolder) in foundDlls)
            {
                var relevantTypes = new List<Type>();
                Diagnostic LoadError(ErrorCode code, params string[] args) => Errors.LoadError(code, args, ProjectManager.MessageSource(target));
                Diagnostic LoadWarning(WarningCode code, params string[] args) => Warnings.LoadWarning(code, args, ProjectManager.MessageSource(target));
                try
                {
                    var typesInAssembly = LoadAssembly(target.LocalPath).GetTypes();
                    var exactInterfaceMatches = typesInAssembly.Where(t => typeof(IRewriteStep).IsAssignableFrom(t)); // inherited interface is defined in this exact dll
                    if (exactInterfaceMatches.Any()) relevantTypes.AddRange(exactInterfaceMatches);
                    else
                    {
                        // If the inherited interface is defined in older compiler version, then we can attempt to load the step anyway via reflection. 
                        // However, in this case we have to load the corresponding assembly into the current context, which can have its own issues. 
                        // We hence first check if this may be the case, and if so we proceed to attempt the loading via reflection. 
                        static bool IsPossibleMatch(Type t) => t.GetInterfaces().Any(t => t.FullName == typeof(IRewriteStep).FullName);
                        var possibleInterfaceMatches = typesInAssembly.Where(IsPossibleMatch);
                        if (possibleInterfaceMatches.Any())
                        {
                            var reloadedTypes = Assembly.LoadFrom(target.LocalPath).GetTypes();
                            relevantTypes.AddRange(reloadedTypes.Where(IsPossibleMatch));
                        }
                    }
                }
                catch (BadImageFormatException ex)
                {
                    onDiagnostic?.Invoke(LoadError(ErrorCode.FileIsNotAnAssembly, target.LocalPath));
                    onException?.Invoke(ex);
                }
                catch (Exception ex)
                {
                    onDiagnostic?.Invoke(LoadError(ErrorCode.CouldNotLoadCompilerPlugin, target.LocalPath));
                    onException?.Invoke(ex);
                }
                var loadedSteps = new List<LoadedStep>();
                foreach (var type in relevantTypes)
                {
                    try 
                    {
                        var instance = Activator.CreateInstance(type);
                        if (instance is IRewriteStep step)
                        {
                            loadedSteps.Add(new LoadedStep(step, typeof(IRewriteStep), target));
                            continue;
                        }

                        try // we also try to load rewrite steps that have been compiled against a different compiler version
                        {
                            var interfaceType = type.GetInterfaces().First(t => t.FullName == typeof(IRewriteStep).FullName);
                            var loadedStep = new LoadedStep(instance, interfaceType, target);
                            onDiagnostic?.Invoke(LoadWarning(WarningCode.RewriteStepLoadedViaReflection, loadedStep.Name, target.LocalPath));
                            loadedSteps.Add(loadedStep);
                        }
                        catch // we don't log the exception, since it is perfectly possible that we should have ignored this type in the first place
                        {
                            onDiagnostic?.Invoke(LoadWarning(WarningCode.FailedToLoadRewriteStepViaReflection, target.LocalPath));
                        }
                    }
                    catch (Exception ex)
                    {
                        onDiagnostic?.Invoke(LoadError(ErrorCode.CouldNotInstantiateRewriteStep, type.ToString(), target.LocalPath));
                        onException?.Invoke(ex);
                    }
                }
                foreach (var loaded in loadedSteps)
                {
                    var assemblyConstants = loaded.AssemblyConstants;
                    if (assemblyConstants == null) continue;
                    foreach (var kvPair in config.AssemblyConstants ?? Enumerable.Empty<KeyValuePair<string, string>>())
                    { assemblyConstants[kvPair.Key] = kvPair.Value; } 

                    var defaultOutput = assemblyConstants.TryGetValue(AssemblyConstants.OutputPath, out var path) ? path : null; 
                    assemblyConstants[AssemblyConstants.OutputPath] = outputFolder ?? defaultOutput ?? config.BuildOutputFolder;
                    assemblyConstants[AssemblyConstants.AssemblyName] = config.ProjectNameWithoutExtension;
                }

                loadedSteps.Sort((fst, snd) => snd.Priority - fst.Priority);
                rewriteSteps.AddRange(loadedSteps);
            }
            return rewriteSteps.ToImmutableArray();
        }
    }
}
