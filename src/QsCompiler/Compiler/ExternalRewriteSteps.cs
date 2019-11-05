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
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler
{
    internal static class RewriteSteps
    {
        /// <summary>
        /// Concrete implementation of the rewrite steps options such that the configured options may be null. 
        /// </summary>
        internal class RewriteStepOptions : IRewriteStepOptions
        {
            public readonly string OutputFolder;
            internal RewriteStepOptions(CompilationLoader.Configuration config) =>
                this.OutputFolder = config.BuildOutputFolder;
        }

        /// <summary>
        /// Concrete implementation of a rewrite steps with an additional property specifying the dll it was loaded from. 
        /// </summary>
        internal class LoadedStep : IRewriteStep
        {
            internal readonly Uri Origin;
            private readonly Func<QsCompilation, (bool, QsCompilation)> _Transformation;
            private readonly Func<QsCompilation, bool> _PreconditionVerification;
            private readonly Func<QsCompilation, bool> _PostconditionVerification;

            internal LoadedStep(IRewriteStep step, Uri origin)
            {
                this.Origin = origin ?? throw new ArgumentNullException(nameof(origin));
                this.Priority = step?.Priority ?? throw new ArgumentNullException(nameof(step));
                this.Name = step.Name;
                this.Options = step.Options;
                this.ImplementsPreconditionVerification = step.ImplementsPreconditionVerification;
                this.ImplementsPostconditionVerification = step.ImplementsPostconditionVerification;
                this.ImplementsTransformation = step.ImplementsTransformation;
                this._PreconditionVerification = step.PreconditionVerification;
                this._PostconditionVerification = step.PostconditionVerification;
                this._Transformation = compilation => step.Transformation(compilation, out var transformed) ? (true, transformed) : (false, compilation);
            }

            /// <summary>
            /// Attempts to construct a rewrite step via reflection. 
            /// Throws the corresponding exception if that construction fails. 
            /// </summary>
            internal LoadedStep(object step, Uri origin)
            {
                var type = step?.GetType() ?? throw new ArgumentNullException(nameof(step));
                this.Origin = origin ?? throw new ArgumentNullException(nameof(origin));
                this.Priority = (int)type.GetProperty(nameof(IRewriteStep.Priority)).GetValue(step);
                this.Name = (string)type.GetProperty(nameof(IRewriteStep.Name)).GetValue(step);
                this.Options = (IRewriteStepOptions)type.GetProperty(nameof(IRewriteStep.Options)).GetValue(step); // todo: handle custom setters?
                this.ImplementsPreconditionVerification = (bool)type.GetProperty(nameof(IRewriteStep.ImplementsPreconditionVerification)).GetValue(step);
                this.ImplementsPostconditionVerification = (bool)type.GetProperty(nameof(IRewriteStep.ImplementsPostconditionVerification)).GetValue(step);
                this.ImplementsTransformation = (bool)type.GetProperty(nameof(IRewriteStep.ImplementsTransformation)).GetValue(step);

                // note that the loading via reflection will have the consequence that the methods may fail on execution
                this._PreconditionVerification = compilation => (bool)type.GetMethod(nameof(IRewriteStep.PreconditionVerification)).Invoke(step, new[] { compilation }); 
                this._PostconditionVerification = compilation => (bool)type.GetMethod(nameof(IRewriteStep.PostconditionVerification)).Invoke(step, new[] { compilation });
                this._Transformation = compilation =>
                {
                    var args = new object[] { compilation, null };
                    var success = (bool)type.GetMethod(nameof(IRewriteStep.Transformation)).Invoke(step, args);
                    return success ? (true, (QsCompilation)args[1]) : (false, compilation);
                };
            }

            public string Name { get; }
            public int Priority { get; }
            public IRewriteStepOptions Options { get; set; }

            public bool ImplementsTransformation { get; }
            public bool ImplementsPreconditionVerification { get; }
            public bool ImplementsPostconditionVerification { get; }

            public bool PreconditionVerification(QsCompilation compilation) =>
                this._PreconditionVerification(compilation);

            public bool PostconditionVerification(QsCompilation compilation) =>
                this._PostconditionVerification(compilation);

            public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
            {
                var (status, res) = this._Transformation(compilation);
                transformed = res;
                return status;
            }
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
            Uri WithFullPath(string file)
            {
                try { return String.IsNullOrWhiteSpace(file) ? null : new Uri(Path.GetFullPath(file)); }
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
            { onDiagnostic?.Invoke(Errors.LoadError(ErrorCode.UnknownCompilerPlugin, new[] { file.LocalPath }, file.LocalPath)); }

            var rewriteSteps = ImmutableArray.CreateBuilder<LoadedStep>();
            foreach (var (target, rewriteStepOptions) in foundDlls)
            {
                var relevantTypes = new List<Type>();
                Diagnostic LoadError(ErrorCode code, params string[] args) => Errors.LoadError(code, args, ProjectManager.MessageSource(target));
                Diagnostic LoadWarning(WarningCode code, params string[] args) => Warnings.LoadWarning(code, args, ProjectManager.MessageSource(target));
                try
                {
                    var asmType = Assembly.LoadFrom(target.LocalPath).GetTypes();
                    var exactInterfaceMatch = asmType.Where(t => typeof(IRewriteStep).IsAssignableFrom(t)); // inherited interface is defined in this exact dll
                    var compatibleInterface = asmType.Where(t => t.GetInterfaces().Any(t => t.FullName == typeof(IRewriteStep).FullName)); // inherited interface may be defined in older compiler version
                    relevantTypes.AddRange(exactInterfaceMatch.Concat(compatibleInterface)); 
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
                            loadedSteps.Add(new LoadedStep(step, target));
                            continue;
                        }

                        try // we also try to load rewrite steps that have been compiled against a different compiler version
                        {
                            var loadedStep = new LoadedStep(instance, target);
                            onDiagnostic?.Invoke(LoadWarning(WarningCode.RewriteStepLoadedViaReflection, loadedStep.Name, target.LocalPath));
                            loadedSteps.Add(loadedStep);
                        }
                        catch // we don't log the exception, since it is perfectly possible that we should have ignored this type in the first place
                        { onDiagnostic?.Invoke(LoadWarning(WarningCode.FailedToLoadRewriteStepViaReflection, target.LocalPath)); }
                    }
                    catch (Exception ex)
                    {
                        onDiagnostic?.Invoke(LoadError(ErrorCode.CouldNotInstantiateRewriteStep, type.ToString(), target.LocalPath));
                        onException?.Invoke(ex);
                    }
                }
                foreach (var loaded in loadedSteps)
                { loaded.Options = rewriteStepOptions ?? loaded.Options ?? config.RewriteStepDefaultOptions; }

                loadedSteps.Sort((fst, snd) => snd.Priority - fst.Priority);
                rewriteSteps.AddRange(loadedSteps);
            }
            return rewriteSteps.ToImmutableArray();
        }
    }
}
