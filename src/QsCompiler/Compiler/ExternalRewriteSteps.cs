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
using Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Quantum.QsCompiler
{
    internal static class ExternalRewriteSteps
    {
        internal static ImmutableArray<IRewriteStep> LoadRewriteSteps(CompilationLoader.Configuration config,
            Action<Diagnostic> onDiagnostic = null, Action<Exception> onException = null)
        {
            if (config.RewriteSteps == null) return ImmutableArray<IRewriteStep>.Empty;
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

            var rewriteSteps = ImmutableArray.CreateBuilder<IRewriteStep>();
            foreach (var (target, rewriteStepOptions) in foundDlls)
            {
                var relevantTypes = new List<Type>();
                Diagnostic LoadError(ErrorCode code, params string[] args) => Errors.LoadError(code, args, ProjectManager.MessageSource(target));
                try { relevantTypes.AddRange(Assembly.LoadFrom(target.LocalPath).GetTypes().Where(t => t.IsAssignableFrom(typeof(IRewriteStep)))); }
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
                var loadedSteps = new List<IRewriteStep>();
                foreach (var type in relevantTypes)
                {
                    try { loadedSteps.Add((IRewriteStep)Activator.CreateInstance(type)); }
                    catch (Exception ex)
                    {
                        onDiagnostic?.Invoke(LoadError(ErrorCode.CouldNotInstantiateRewriteStep, type.ToString(), target.LocalPath)); // todo: check message
                        onException?.Invoke(ex);
                    }
                }
                foreach (var loaded in loadedSteps)
                { loaded.Options = rewriteStepOptions ?? loaded.Options ?? config.RewriteStepDefaultOptions; }

                loadedSteps.Sort((fst, snd) => snd.Priority - fst.Priority); // fixme: check ordering
                rewriteSteps.AddRange(loadedSteps);
            }
            return rewriteSteps.ToImmutableArray();
        }
    }
}
