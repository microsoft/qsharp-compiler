// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsCompiler
{
    internal abstract class AbstractRewriteStepsLoader
    {
        protected Action<Diagnostic>? OnDiagnostic { get; }

        protected Action<Exception>? OnException { get; }

        protected AbstractRewriteStepsLoader(Action<Diagnostic>? onDiagnostic = null, Action<Exception>? onException = null)
        {
            this.OnDiagnostic = onDiagnostic;
            this.OnException = onException;
        }

        protected Diagnostic LoadError(Uri target, ErrorCode code, params string[] args) => Errors.LoadError(code, args, ProjectManager.MessageSource(target));

        protected Diagnostic LoadWarning(Uri target, WarningCode code, params string[] args) => Warnings.LoadWarning(code, args, ProjectManager.MessageSource(target));

        protected LoadedStep? CreateStep(Type type, Uri target, string? outputFolder)
        {
            try
            {
                var instance = Activator.CreateInstance(type);
                if (instance is IRewriteStep step)
                {
                    return new LoadedStep(step, typeof(IRewriteStep), target, outputFolder);
                }

                // we also try to load rewrite steps that have been compiled against a different compiler version
                try
                {
                    var interfaceType = type.GetInterfaces().First(t => t.FullName == typeof(IRewriteStep).FullName);
                    var loadedStep = new LoadedStep(instance, interfaceType, target, outputFolder);
                    this.OnDiagnostic?.Invoke(this.LoadWarning(target, WarningCode.RewriteStepLoadedViaReflection, loadedStep.Name, target.LocalPath));
                    return loadedStep;
                }
                catch
                {
                    // we don't log the exception, since it is perfectly possible that we should have ignored this type in the first place
                    this.OnDiagnostic?.Invoke(this.LoadWarning(target, WarningCode.FailedToLoadRewriteStepViaReflection, target.LocalPath));
                }
            }
            catch (Exception ex)
            {
                this.OnDiagnostic?.Invoke(this.LoadError(target, ErrorCode.CouldNotInstantiateRewriteStep, type.ToString(), target.LocalPath));
                this.OnException?.Invoke(ex);
            }

            return null;
        }
    }
}
