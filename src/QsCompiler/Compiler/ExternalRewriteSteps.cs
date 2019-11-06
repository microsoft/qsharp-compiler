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
        /// Concrete implementation of a rewrite steps with an additional property specifying the dll it was loaded from. 
        /// </summary>
        internal class LoadedStep : IRewriteStep
        {
            internal readonly Uri Origin;
            private readonly object _internalObject;
            private readonly Type _internalType;

            /// <summary>
            /// Attempts to construct a rewrite step via reflection.
            /// Note that the loading via reflection has the consequence that methods may fail on execution. 
            /// This is e.g. the case if they invoke methods from package references if the corresponding dll 
            /// has not been copied to output folder of the dll from which the rewrite step is loaded. 
            /// Throws the corresponding exception if that construction fails. 
            /// </summary>
            internal LoadedStep(object step, Uri origin)
            {
                _internalObject = step;
                _internalType = step?.GetType() ?? throw new ArgumentNullException(nameof(step));

                this.Origin = origin ?? throw new ArgumentNullException(nameof(origin));
            }

            public string Name
            {
                get => (string)_internalType.GetProperty(nameof(IRewriteStep.Name)).GetValue(_internalObject);
            }

            public int Priority
            {
                get => (int)_internalType.GetProperty(nameof(IRewriteStep.Priority)).GetValue(_internalObject);
            }

            public string OutputFolder
            {
                get => (string)_internalType.GetProperty(nameof(IRewriteStep.OutputFolder)).GetValue(_internalObject);
                set => _internalType.GetProperty(nameof(IRewriteStep.OutputFolder)).SetValue(_internalObject, value);
            }

            public bool ImplementsTransformation
            {
                get => (bool)_internalType.GetProperty(nameof(IRewriteStep.ImplementsTransformation)).GetValue(_internalObject);
            }

            public bool ImplementsPreconditionVerification
            {
                get => (bool)_internalType.GetProperty(nameof(IRewriteStep.ImplementsPreconditionVerification)).GetValue(_internalObject);
            }

            public bool ImplementsPostconditionVerification
            {
                get => (bool)_internalType.GetProperty(nameof(IRewriteStep.ImplementsPostconditionVerification)).GetValue(_internalObject);
            }

            public bool Transformation(QsCompilation compilation, out QsCompilation transformed)
            {
                var args = new object[] { compilation, null };
                bool success = (bool)_internalType.GetMethod(nameof(IRewriteStep.Transformation)).Invoke(_internalObject, args);
                transformed = success ? (QsCompilation)args[1] : compilation;
                return success;
            }

            public bool PreconditionVerification(QsCompilation compilation)
            {
                return (bool)_internalType.GetMethod(nameof(IRewriteStep.PreconditionVerification)).Invoke(_internalObject, new[] { compilation });
            }

            public bool PostconditionVerification(QsCompilation compilation)
            {
                return (bool)_internalType.GetMethod(nameof(IRewriteStep.PostconditionVerification)).Invoke(_internalObject, new[] { compilation });
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
                    var interfaceMatch = Assembly.LoadFrom(target.LocalPath).GetTypes().Where(
                        t => typeof(IRewriteStep).IsAssignableFrom(t) || // inherited interface is defined in this exact dll
                        t.GetInterfaces().Any(t => t.FullName == typeof(IRewriteStep).FullName)); // inherited interface may be defined in older compiler version
                    relevantTypes.AddRange(interfaceMatch);
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
                    loaded.OutputFolder = outputFolder ?? loaded.OutputFolder ?? config.BuildOutputFolder;
                }

                loadedSteps.Sort((fst, snd) => snd.Priority - fst.Priority);
                rewriteSteps.AddRange(loadedSteps);
            }
            return rewriteSteps.ToImmutableArray();
        }
    }
}
