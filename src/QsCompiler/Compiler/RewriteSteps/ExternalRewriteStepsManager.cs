// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler
{
    internal static class ExternalRewriteStepsManager
    {
        /// <summary>
        /// Loads all dlls listed as containing rewrite steps to include in the compilation process in the given configuration.
        /// Generates suitable diagnostics if a listed file can't be found or loaded.
        /// Finds all types implementing the IRewriteStep interface and loads the corresponding rewrite steps
        /// according to the specified priority, where a steps with a higher priority will be listed first in the returned array.
        /// If the function onDiagnostic is specified and not null, calls it on all generated diagnostics,
        /// and calls onException on all caught exceptions if it is specified and not null.
        /// Returns an empty array if the rewrite steps in the given configurations are set to null.
        /// </summary>
        internal static ImmutableArray<LoadedStep> Load(CompilationLoader.Configuration config, Action<Diagnostic>? onDiagnostic = null, Action<Exception>? onException = null)
        {
            var instanceRewriteStepLoader = new InstanceRewriteStepsLoader(onDiagnostic, onException);
            var typeRewriteStepLoader = new TypeRewriteStepsLoader(onDiagnostic, onException);
            var assemblyRewriteStepLoader = new AssemblyRewriteStepsLoader(onDiagnostic, onException);

            var loadedSteps = instanceRewriteStepLoader.GetLoadedSteps(config.RewriteStepInstances)
                .Concat(typeRewriteStepLoader.GetLoadedSteps(config.RewriteStepTypes))
                .Concat(assemblyRewriteStepLoader.GetLoadedSteps(config.RewriteStepAssemblies))
                .OrderByDescending(step => step.Priority)
                .ToImmutableArray();

            foreach (var loaded in loadedSteps)
            {
                var assemblyConstants = loaded.AssemblyConstants;
                if (assemblyConstants == null)
                {
                    continue;
                }

                var updateConstants = config.SkipTargetSpecificCompilation
                    ? ImmutableDictionary.CreateRange(new[]
                    {
                        KeyValuePair.Create(AssemblyConstants.ExecutionTarget, "Any"),
                        KeyValuePair.Create(AssemblyConstants.ProcessorArchitecture, "Unspecified"),
                        KeyValuePair.Create(AssemblyConstants.TargetCapability, TargetCapabilityModule.Top.Name),
                    })
                    : ImmutableDictionary<string, string>.Empty;
                foreach (var kvPair in config.AssemblyConstants ?? Enumerable.Empty<KeyValuePair<string, string>>())
                {
                    assemblyConstants[kvPair.Key] = updateConstants.TryGetValue(kvPair.Key, out var value) ? value : kvPair.Value;
                }

                // We don't overwrite assembly properties specified by configuration.
                var defaultOutput = assemblyConstants.TryGetValue(AssemblyConstants.OutputPath, out var path) ? path : null;
                assemblyConstants[AssemblyConstants.OutputPath] = loaded.OutputFolder ?? defaultOutput ?? config.BuildOutputFolder;
                assemblyConstants.TryAdd(AssemblyConstants.AssemblyName, config.ProjectNameWithoutPathOrExtension);

                if (config.TargetPackageAssemblies != null)
                {
                    string DefinedTargetPackageAssemblies(string? predefined = null) =>
                        string.Join(";", config.TargetPackageAssemblies.Append(predefined).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s?.Trim()));
                    assemblyConstants[AssemblyConstants.TargetPackageAssemblies] = DefinedTargetPackageAssemblies(
                        assemblyConstants.TryGetValue(AssemblyConstants.TargetPackageAssemblies, out var predefined) ? predefined : null);
                }
            }

            return loadedSteps;
        }
    }
}
