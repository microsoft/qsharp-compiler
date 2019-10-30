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
            private readonly IRewriteStep Implementation;
            internal readonly Uri Origin;

            internal LoadedStep(IRewriteStep step, Uri origin)
            {
                this.Implementation = step ?? throw new ArgumentNullException(nameof(step));
                this.Origin = origin ?? throw new ArgumentNullException(nameof(origin)); 
            }

            public string Name => this.Implementation.Name;
            public int Priority => this.Implementation.Priority;
            public IRewriteStepOptions Options 
            { 
                get => this.Implementation.Options; 
                set { this.Implementation.Options = value; }
            }

            public bool ImplementsTransformation => this.Implementation.ImplementsTransformation;
            public bool ImplementsPreconditionVerification => this.Implementation.ImplementsPreconditionVerification;
            public bool ImplementsPostconditionVerification => this.Implementation.ImplementsPostconditionVerification;

            public bool PostconditionVerification(QsCompilation compilation) =>
                this.Implementation.PostconditionVerification(compilation);

            public bool PreconditionVerification(QsCompilation compilation) =>
                this.Implementation.PreconditionVerification(compilation);

            public bool Transformation(QsCompilation compilation, out QsCompilation transformed) =>
                this.Implementation.Transformation(compilation, out transformed);
        }


        /// <summary>
        /// ...
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
                var loadedSteps = new List<LoadedStep>();
                foreach (var type in relevantTypes)
                {
                    try { loadedSteps.Add(new LoadedStep((IRewriteStep)Activator.CreateInstance(type), target)); }
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
