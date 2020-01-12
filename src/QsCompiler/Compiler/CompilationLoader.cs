// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Bson;
using MetadataReference = Microsoft.CodeAnalysis.MetadataReference;


namespace Microsoft.Quantum.QsCompiler
{
    public class CompilationLoader
    {
        /// <summary>
        /// Given a load function that loads the content of a sequence of files from disk, 
        /// returns the content for all sources to compile. 
        /// </summary>
        public delegate ImmutableDictionary<Uri, string> SourceLoader(Func<IEnumerable<string>, ImmutableDictionary<Uri, string>> loadFromDisk);
        /// <summary>
        /// Given a load function that loads the content of a sequence of referenced assemblies from disk,
        /// returns the loaded references for the compilation. 
        /// </summary>
        public delegate References ReferenceLoader(Func<IEnumerable<string>, References> loadFromDisk);
        /// <summary>
        /// If LoadAssembly is not null, it will be used to load the dlls that are search for classes defining rewrite steps.
        /// </summary>
        public static Func<string, Assembly> LoadAssembly { get; set; }


        /// <summary>
        /// may be specified via configuration (or project) file in the future
        /// </summary>
        public struct Configuration
        {
            /// <summary>
            /// The name of the project. Used as assembly name in the generated dll. 
            /// The name of the project with a suitable extension will also be used as the name of the generated binary file.
            /// </summary>
            public string ProjectName;
            /// <summary>
            /// If set to true, the syntax tree rewrite step that replaces all generation directives 
            /// for all functor specializations is executed during compilation.   
            /// </summary>
            public bool GenerateFunctorSupport;
            /// <summary>
            /// Unless this is set to true, the syntax tree rewrite step that eliminates selective abstractions is executed during compilation. 
            /// In particular, all conjugations are inlined. 
            /// </summary>
            public bool SkipSyntaxTreeTrimming;
            /// <summary>
            /// If set to true, the compiler attempts to pre-evaluate the built compilation as much as possible.
            /// This is an experimental feature that will change over time. 
            /// </summary>
            public bool AttemptFullPreEvaluation;
            /// <summary>
            /// Unless this is set to true, all usages of type-parameterized callables are replaced with 
            /// the concrete callable instantiation if an entry point is specified for the compilation.
            /// Removes all type-parameterizations in the syntax tree.
            /// </summary>
            public bool SkipMonomorphization;
            /// <summary>
            /// If the output folder is not null, 
            /// documentation is generated in the specified folder based on doc comments in the source code. 
            /// </summary>
            public string DocumentationOutputFolder;
            /// <summary>
            /// Directory where the compiled binaries will be generated. 
            /// No binaries will be written to disk unless this path is specified and valid. 
            /// </summary>
            public string BuildOutputFolder;
            /// <summary>
            /// Output path for the dll containing the compiled binaries. 
            /// No dll will be generated unless this path is specified and valid. 
            /// </summary>
            public string DllOutputPath;
            /// <summary>
            /// If set to true, then referenced dlls will be loaded purely based on attributes in the contained C# code. 
            /// Any Q# resources will be ignored. 
            /// </summary>
            public bool LoadReferencesBasedOnGeneratedCsharp;
            /// <summary>
            /// Contains a sequence of tuples with the path to a dotnet dll containing one or more rewrite steps 
            /// (i.e. classes implementing IRewriteStep) and the corresponding output folder.
            /// The contained rewrite steps will be executed in the defined order and priority at the end of the compilation. 
            /// </summary>
            public IEnumerable<(string, string)> RewriteSteps;
            /// <summary>
            /// If set to true, the post-condition for loaded rewrite steps is checked if the corresponding verification is implemented.
            /// Otherwise post-condition verifications are skipped. 
            /// </summary>
            public bool EnableAdditionalChecks;
            /// <summary>
            /// Handle to pass arbitrary constants with which to populate the corresponding dictionary for loaded rewrite steps. 
            /// These values will take precedence over any already existing values that the default constructor sets. 
            /// However, the compiler may overwrite the assembly constants defined for the Q# compilation unit in the dictionary of the loaded step.
            /// The given dictionary in this configuration is left unchanged in any case. 
            /// </summary>
            public IReadOnlyDictionary<string, string> AssemblyConstants; 

            /// <summary>
            /// Indicates whether a serialization of the syntax tree needs to be generated. 
            /// This is the case if either the build output folder is specified or the dll output path is specified.
            /// </summary>
            internal bool SerializeSyntaxTree =>
                BuildOutputFolder != null || DllOutputPath != null;

            /// <summary>
            /// If the ProjectName does not have an ending "proj", appends a .qsproj ending to the project name. 
            /// Returns null if the project name is null. 
            /// </summary>
            internal string ProjectNameWithExtension =>
                this.ProjectName == null ? null :
                this.ProjectName.EndsWith("proj") ? this.ProjectName : 
                $"{this.ProjectName}.qsproj";

            /// <summary>
            /// If the ProjectName does have an extension ending with "proj", returns the project name without that extension. 
            /// Returns null if the project name is null. 
            /// </summary>
            internal string ProjectNameWithoutExtension =>
                this.ProjectName == null ? null :
                Path.GetExtension(this.ProjectName).EndsWith("proj") ? Path.GetFileNameWithoutExtension(this.ProjectName) :
                this.ProjectName;
        }

        /// <summary>
        /// used to indicate the status of individual compilation steps
        /// </summary>
        public enum Status { NotRun = -1, Succeeded = 0, Failed = 1 }

        private class ExecutionStatus
        {
            internal Status SourceFileLoading = Status.NotRun;
            internal Status ReferenceLoading = Status.NotRun;
            internal Status PluginLoading = Status.NotRun;
            internal Status Validation = Status.NotRun;
            internal Status FunctorSupport = Status.NotRun;
            internal Status PreEvaluation = Status.NotRun;
            internal Status TreeTrimming = Status.NotRun;
            internal Status Monomorphization = Status.NotRun;
            internal Status Documentation = Status.NotRun;
            internal Status Serialization = Status.NotRun;
            internal Status BinaryFormat = Status.NotRun;
            internal Status DllGeneration = Status.NotRun;
            internal Status[] LoadedRewriteSteps;

            internal ExecutionStatus(IEnumerable<IRewriteStep> externalRewriteSteps) =>
                this.LoadedRewriteSteps = externalRewriteSteps.Select(_ => Status.NotRun).ToArray();

            private bool WasSuccessful(bool run, Status code) =>
                (run && code == Status.Succeeded) || (!run && code == Status.NotRun);

            internal bool Success(Configuration options, bool isExe) =>
                this.SourceFileLoading <= 0 &&
                this.ReferenceLoading <= 0 &&
                WasSuccessful(true, this.Validation) &&
                WasSuccessful(true, this.PluginLoading) &&
                WasSuccessful(options.GenerateFunctorSupport, this.FunctorSupport) &&
                WasSuccessful(options.AttemptFullPreEvaluation, this.PreEvaluation) &&
                WasSuccessful(!options.SkipSyntaxTreeTrimming, this.TreeTrimming) &&
                WasSuccessful(isExe && !options.SkipMonomorphization, this.Monomorphization) &&
                WasSuccessful(options.DocumentationOutputFolder != null, this.Documentation) &&
                WasSuccessful(options.SerializeSyntaxTree, this.Serialization) &&
                WasSuccessful(options.BuildOutputFolder != null, this.BinaryFormat) &&
                WasSuccessful(options.DllOutputPath != null, this.DllGeneration) &&
                this.LoadedRewriteSteps.All(status => WasSuccessful(true, status));
        }

        /// <summary>
        /// Indicates whether all source files were loaded successfully.
        /// Source file loading may not be executed if the content was preloaded using methods outside this class. 
        /// </summary>
        public Status SourceFileLoading => this.CompilationStatus.SourceFileLoading;
        /// <summary>
        /// Indicates whether all references were loaded successfully.
        /// The loading may not be executed if all references were preloaded using methods outside this class. 
        /// </summary>
        public Status ReferenceLoading => this.CompilationStatus.ReferenceLoading;
        /// <summary>
        /// Indicates whether all external dlls specifying e.g. rewrite steps 
        /// to perform as part of the compilation have been loaded successfully.
        /// The status indicates a successful execution if no such external dlls have been specified. 
        /// </summary>
        public Status PluginLoading => this.CompilationStatus.PluginLoading;
        /// <summary>
        /// Indicates whether the compilation unit passed the compiler validation 
        /// that is executed before invoking further rewrite and/or generation steps.   
        /// </summary>
        public Status Validation => this.CompilationStatus.Validation;
        /// <summary>
        /// Indicates whether all specializations were generated successfully. 
        /// This rewrite step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status FunctorSupport => this.CompilationStatus.FunctorSupport;
        /// <summary>
        /// Indicates whether the pre-evaluation step executed successfully. 
        /// This rewrite step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status PreEvaluation => this.CompilationStatus.PreEvaluation;
        /// <summary>
        /// Indicates whether all the type-parameterized callables were resolved to concrete callables.
        /// This rewrite step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status Monomorphization => this.CompilationStatus.Monomorphization;
        /// <summary>
        /// Indicates whether documentation for the compilation was generated successfully. 
        /// This step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status Documentation => this.CompilationStatus.Documentation;
        /// <summary>
        /// Indicates whether the built compilation could be serialized successfully. 
        /// This step is only executed if either the binary representation or a dll is emitted. 
        /// </summary>
        public Status Serialization => this.CompilationStatus.Serialization;
        /// <summary>
        /// Indicates whether a binary representation for the generated syntax tree has been generated successfully. 
        /// This step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status BinaryFormat => this.CompilationStatus.BinaryFormat;
        /// <summary>
        /// Indicates whether a dll containing the compiled binary has been generated successfully. 
        /// This step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status DllGeneration => this.CompilationStatus.DllGeneration;

        /// <summary>
        /// Indicates whether all rewrite steps with the given name and loaded from the given source executed successfully. 
        /// The source, if specified, is the path to the dll in which the step is specified.
        /// Returns a status NotRun if no such step was found or executed. 
        /// Execution is considered successful if the precondition and transformation (if any) returned true. 
        /// </summary>
        public Status LoadedRewriteStep(string name, string source = null)
        {
            var uri = String.IsNullOrWhiteSpace(source) ? null : new Uri(Path.GetFullPath(source));
            bool MatchesQuery(int index) => this.ExternalRewriteSteps[index].Name == name && (source == null || this.ExternalRewriteSteps[index].Origin == uri);
            var statuses = this.CompilationStatus.LoadedRewriteSteps.Where((s, i) => MatchesQuery(i)).ToArray();
            return statuses.All(s => s == Status.Succeeded) ? Status.Succeeded : statuses.Any(s => s == Status.Failed) ? Status.Failed : Status.NotRun;
        }
        /// <summary>
        /// Indicates the overall status of all rewrite step from external dlls.
        /// The status is indicated as success if none of these steps failed. 
        /// </summary>
        public Status AllLoadedRewriteSteps => this.CompilationStatus.LoadedRewriteSteps.Any(s => s == Status.Failed) ? Status.Failed : Status.Succeeded;
        /// <summary>
        /// Indicates the overall success of all compilation steps. 
        /// The compilation is indicated as having been successful if all steps that were configured to execute completed successfully.
        /// </summary>
        public bool Success => this.CompilationStatus.Success(this.Config, this.CompilationOutput?.EntryPoints.Length != 0);


        /// <summary>
        /// Logger used to log all diagnostic events during compilation.
        /// </summary>
        private readonly ILogger Logger;
        /// <summary>
        /// Configuration specifying the compilation steps to execute.
        /// </summary>
        private readonly Configuration Config;
        /// <summary>
        /// Used to track the status of individual compilation steps.
        /// </summary>
        private readonly ExecutionStatus CompilationStatus;
        /// <summary>
        /// Contains all loaded rewrite steps found in the specified plugin dlls, 
        /// where configurable properties such as the output folder have already been initialized to suitable values. 
        /// </summary>
        private readonly ImmutableArray<RewriteSteps.LoadedStep> ExternalRewriteSteps;

        /// <summary>
        /// Contains all diagnostics generated upon source file and reference loading.
        /// All other diagnostics can be accessed via the VerifiedCompilation.
        /// </summary>
        public ImmutableArray<Diagnostic> LoadDiagnostics;
        /// <summary>
        /// Contains the initial compilation built by the compilation unit manager after verification.
        /// </summary>
        public readonly CompilationUnitManager.Compilation VerifiedCompilation;
        /// <summary>
        /// Contains the built compilation including the syntax tree after executing all configured rewrite steps.
        /// </summary>
        public readonly QsCompilation CompilationOutput;
        /// <summary>
        /// Contains the absolute path where the binary representation of the generated syntax tree has been written to disk.
        /// </summary>
        public readonly string PathToCompiledBinary;
        /// <summary>
        /// Contains the absolute path where the generated dll containing the compiled binary has been written to disk.
        /// </summary>
        public readonly string DllOutputPath;

        /// <summary>
        /// Contains the full Q# syntax tree after executing all configured rewrite steps, including the content of loaded references. 
        /// </summary>
        public IEnumerable<QsNamespace> GeneratedSyntaxTree =>
            this.CompilationOutput?.Namespaces;

        /// <summary>
        /// Contains the Uri and names of all rewrite steps loaded from the specified dlls 
        /// in the order in which they are executed. 
        /// </summary>
        public ImmutableArray<(Uri, string)> LoadedRewriteSteps =>
            this.ExternalRewriteSteps.Select(step => (step.Origin, step.Name)).ToImmutableArray();


        /// <summary>
        /// Builds the compilation for the source files and references loaded by the given loaders,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events. 
        /// Throws an ArgumentNullException if either one of the given loaders is null or returns null.
        /// </summary>
        public CompilationLoader(SourceLoader loadSources, ReferenceLoader loadReferences, Configuration? options = null, ILogger logger = null)
        {
            // loading the content to compiler 

            this.Logger = logger;
            this.LoadDiagnostics = ImmutableArray<Diagnostic>.Empty;
            this.Config = options ?? new Configuration();

            // We load all referenced .NET assemblies into the current context to 
            // for the sake of having a more resilient setup for loading rewrite steps.
            loadReferences?.Invoke(refs =>
            {
                // no need to generate errors - this step is just a precaution
                foreach (var dllPath in refs)
                {
                    try { Assembly.LoadFrom(dllPath); }
                    catch { continue; }
                }
                return References.Empty;
            });

            Status rewriteStepLoading = Status.Succeeded;
            this.ExternalRewriteSteps = RewriteSteps.Load(this.Config,
                d => this.LogAndUpdateLoadDiagnostics(ref rewriteStepLoading, d),
                ex => this.LogAndUpdate(ref rewriteStepLoading, ex));
            this.PrintLoadedRewriteSteps(this.ExternalRewriteSteps);
            this.CompilationStatus = new ExecutionStatus(this.ExternalRewriteSteps);
            this.CompilationStatus.PluginLoading = rewriteStepLoading;

            var sourceFiles = loadSources?.Invoke(this.LoadSourceFiles) 
                ?? throw new ArgumentNullException("unable to load source files");
            var references = loadReferences?.Invoke(refs => this.LoadAssemblies(refs, this.Config.LoadReferencesBasedOnGeneratedCsharp)) 
                ?? throw new ArgumentNullException("unable to load referenced binary files");

            // building the compilation

            this.CompilationStatus.Validation = Status.Succeeded;
            var files = CompilationUnitManager.InitializeFileManagers(sourceFiles, null, this.OnCompilerException); // do *not* live track (i.e. use publishing) here!
            var compilationManager = new CompilationUnitManager(this.OnCompilerException);
            compilationManager.UpdateReferencesAsync(references);
            compilationManager.AddOrUpdateSourceFilesAsync(files);
            this.VerifiedCompilation = compilationManager.Build();
            this.CompilationOutput = this.VerifiedCompilation?.BuiltCompilation;
            compilationManager.Dispose();

            foreach (var diag in this.VerifiedCompilation?.Diagnostics() ?? Enumerable.Empty<Diagnostic>())
            { this.LogAndUpdate(ref this.CompilationStatus.Validation, diag); }

            // executing the specified rewrite steps 

            if (!this.Config.SkipMonomorphization && this.CompilationOutput?.EntryPoints.Length != 0)
            {
                if (!Uri.TryCreate(Assembly.GetExecutingAssembly().CodeBase, UriKind.Absolute, out Uri thisDllUri))
                { thisDllUri = new Uri(Path.GetFullPath(".", "CompilationLoader.cs")); }
                var rewriteStep = new RewriteSteps.LoadedStep(new Monomorphization(), typeof(IRewriteStep), thisDllUri);
                this.CompilationStatus.Monomorphization = this.ExecuteRewriteStep(rewriteStep, this.CompilationOutput, out this.CompilationOutput); 
            }

            if (this.Config.GenerateFunctorSupport)
            {
                this.CompilationStatus.FunctorSupport = Status.Succeeded;
                void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.FunctorSupport, ex);
                var generated = this.CompilationOutput != null && CodeGeneration.GenerateFunctorSpecializations(this.CompilationOutput, out this.CompilationOutput, onException);
                if (!generated) this.LogAndUpdate(ref this.CompilationStatus.FunctorSupport, ErrorCode.FunctorGenerationFailed, Enumerable.Empty<string>());
            }

            if (!this.Config.SkipSyntaxTreeTrimming)
            {
                this.CompilationStatus.TreeTrimming = Status.Succeeded;
                void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.TreeTrimming, ex);
                var trimmed = this.CompilationOutput != null && this.CompilationOutput.InlineConjugations(out this.CompilationOutput, onException);
                if (!trimmed) this.LogAndUpdate(ref this.CompilationStatus.TreeTrimming, ErrorCode.TreeTrimmingFailed, Enumerable.Empty<string>());
            }

            if (this.Config.AttemptFullPreEvaluation)
            {
                this.CompilationStatus.PreEvaluation = Status.Succeeded;
                void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.PreEvaluation, ex);
                var evaluated = this.CompilationOutput != null && this.CompilationOutput.PreEvaluateAll(out this.CompilationOutput, onException);
                if (!evaluated) this.LogAndUpdate(ref this.CompilationStatus.PreEvaluation, ErrorCode.PreEvaluationFailed, Enumerable.Empty<string>());
            }

            // generating the compiled binary and dll

            using (var ms = new MemoryStream())
            {
                var serialized = this.Config.SerializeSyntaxTree && this.SerializeSyntaxTree(ms);
                if (serialized && this.Config.BuildOutputFolder != null)
                { this.PathToCompiledBinary = this.GenerateBinary(ms); }
                if (serialized && this.Config.DllOutputPath != null)
                { this.DllOutputPath = this.GenerateDll(ms); }
            }

            // executing the specified generation steps 

            if (this.Config.DocumentationOutputFolder != null)
            {
                this.CompilationStatus.Documentation = Status.Succeeded;
                var docsFolder = Path.GetFullPath(String.IsNullOrWhiteSpace(this.Config.DocumentationOutputFolder) ? "." : this.Config.DocumentationOutputFolder);
                void onDocException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.Documentation, ex);
                var docsGenerated = this.VerifiedCompilation != null && DocBuilder.Run(docsFolder, this.VerifiedCompilation.SyntaxTree.Values, this.VerifiedCompilation.SourceFiles, onException: onDocException);
                if (!docsGenerated) this.LogAndUpdate(ref this.CompilationStatus.Documentation, ErrorCode.DocGenerationFailed, Enumerable.Empty<string>());
            }

            // invoking rewrite steps in external dlls

            for (int i = 0; i < this.ExternalRewriteSteps.Length; i++)
            {
                if (this.CompilationOutput == null) continue;
                var executed = this.ExecuteRewriteStep(this.ExternalRewriteSteps[i], this.CompilationOutput, out var transformed);
                if (executed == Status.Succeeded) this.CompilationOutput = transformed;
                this.CompilationStatus.LoadedRewriteSteps[i] = executed;
            }
        }

        /// <summary>
        /// Executes the given rewrite step on the given compilation, returning a transformed compilation as an out parameter.
        /// Catches and logs any thrown exception. Returns the status of the rewrite step.
        /// Throws an ArgumentNullException if the rewrite step to execute or the given compilation is null. 
        /// </summary>
        private Status ExecuteRewriteStep(RewriteSteps.LoadedStep rewriteStep, QsCompilation compilation, out QsCompilation transformed)
        {
            if (rewriteStep == null) throw new ArgumentNullException(nameof(rewriteStep));
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            var status = Status.Succeeded;
            var messageSource = ProjectManager.MessageSource(rewriteStep.Origin);
            Diagnostic Warning(WarningCode code, params string[] args) => Warnings.LoadWarning(code, args, messageSource);
            try
            {
                transformed = compilation;
                var preconditionPassed = !rewriteStep.ImplementsPreconditionVerification || rewriteStep.PreconditionVerification(compilation);
                if (!preconditionPassed) this.LogAndUpdate(ref status, Warning(WarningCode.PreconditionVerificationFailed, new[] { rewriteStep.Name, messageSource }));

                var executeTransformation = preconditionPassed && rewriteStep.ImplementsTransformation;
                var transformationPassed = !executeTransformation || rewriteStep.Transformation(compilation, out transformed);
                if (!transformationPassed) this.LogAndUpdate(ref status, ErrorCode.RewriteStepExecutionFailed, new[] { rewriteStep.Name, messageSource });

                var executePostconditionVerification = this.Config.EnableAdditionalChecks && transformationPassed && rewriteStep.ImplementsPostconditionVerification;
                var postconditionPassed = !executePostconditionVerification || rewriteStep.PostconditionVerification(transformed);
                if (!postconditionPassed) this.LogAndUpdate(ref status, ErrorCode.PostconditionVerificationFailed, new[] { rewriteStep.Name, messageSource });
            }
            catch (Exception ex)
            {
                this.LogAndUpdate(ref status, ex);
                var isLoadException = ex is FileLoadException || ex.InnerException is FileLoadException;
                if (isLoadException) this.LogAndUpdate(ref status, ErrorCode.FileNotFoundDuringPluginExecution, new[] { rewriteStep.Name, messageSource });
                else this.LogAndUpdate(ref status, ErrorCode.PluginExecutionFailed, new[] { rewriteStep.Name, messageSource });
                transformed = null;
            }
            return status;
        }

        /// <summary>
        /// Builds the compilation of the specified source files and references,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events. 
        /// </summary>
        public CompilationLoader(IEnumerable<string> sources, IEnumerable<string> references, Configuration? options = null, ILogger logger = null)
            : this(load => load(sources), load => load(references), options, logger) { }

        /// <summary>
        /// Builds the compilation of the specified source files and the loaded references returned by the given loader,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events. 
        /// Throws an ArgumentNullException if the given loader is null or returns null.
        /// </summary>
        public CompilationLoader(IEnumerable<string> sources, ReferenceLoader loadReferences, Configuration? options = null, ILogger logger = null)
            : this(load => load(sources), loadReferences, options, logger) { }

        /// <summary>
        /// Builds the compilation of the content returned by the given loader and the specified references,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events. 
        /// Throws an ArgumentNullException if the given loader is null or returns null.
        /// </summary>
        public CompilationLoader(SourceLoader loadSources, IEnumerable<string> references, Configuration? options = null, ILogger logger = null)
            : this(loadSources, load => load(references), options, logger) { }


        // private routines used for logging and status updates

        /// <summary>
        /// Logs the given diagnostic and updates the status passed as reference accordingly. 
        /// Throws an ArgumentNullException if the given diagnostic is null. 
        /// </summary>
        private void LogAndUpdate(ref Status current, Diagnostic d)
        {
            this.Logger?.Log(d);
            if (d.IsError()) current = Status.Failed;
        }

        /// <summary>
        /// Logs the given exception and updates the status passed as reference accordingly. 
        /// </summary>
        private void LogAndUpdate(ref Status current, Exception ex)
        {
            this.Logger?.Log(ex);
            current = Status.Failed;
        }

        /// <summary>
        /// Logs an error with the given error code and message parameters, and updates the status passed as reference accordingly. 
        /// </summary>
        private void LogAndUpdate(ref Status current, ErrorCode code, IEnumerable<string> args)
        {
            this.Logger?.Log(code, args);
            current = Status.Failed;
        }

        /// <summary>
        /// Logs the given diagnostic and updates the status passed as reference accordingly. 
        /// Adds the given diagnostic to the tracked load diagnostics. 
        /// Throws an ArgumentNullException if the given diagnostic is null. 
        /// </summary>
        private void LogAndUpdateLoadDiagnostics(ref Status current, Diagnostic d)
        {
            this.LoadDiagnostics = this.LoadDiagnostics.Add(d);
            this.LogAndUpdate(ref current, d);
        }

        /// <summary>
        /// Logs an UnexpectedCompilerException error as well as the given exception, and updates the validation status accordingly. 
        /// </summary>
        private void OnCompilerException(Exception ex)
        {
            this.LogAndUpdate(ref this.CompilationStatus.Validation, ErrorCode.UnexpectedCompilerException, Enumerable.Empty<string>());
            this.LogAndUpdate(ref this.CompilationStatus.Validation, ex);
        }

        /// <summary>
        /// Logs the names of the given source files as Information.
        /// Does nothing if the given argument is null.
        /// </summary>
        private void PrintResolvedFiles(IEnumerable<Uri> sourceFiles)
        {
            if (sourceFiles == null) return;
            var args = sourceFiles.Any()
                ? sourceFiles.Select(f => f?.LocalPath).ToArray()
                : new string[] { "(none)" };
            this.Logger?.Log(InformationCode.CompilingWithSourceFiles, Enumerable.Empty<string>(), messageParam: Formatting.Indent(args).ToArray());
        }

        /// <summary>
        /// Logs the names of the given assemblies as Information.
        /// Does nothing if the given argument is null.
        /// </summary>
        private void PrintResolvedAssemblies(IEnumerable<NonNullable<string>> assemblies)
        {
            if (assemblies == null) return;
            var args = assemblies.Any()
                ? assemblies.Select(name => name.Value).ToArray()
                : new string[] { "(none)" };
            this.Logger?.Log(InformationCode.CompilingWithAssemblies, Enumerable.Empty<string>(), messageParam: Formatting.Indent(args).ToArray());
        }

        /// <summary>
        /// Logs the names and origins of the given rewrite steps as Information.
        /// Does nothing if the given argument is null.
        /// </summary>
        private void PrintLoadedRewriteSteps(IEnumerable<RewriteSteps.LoadedStep> rewriteSteps)
        {
            if (rewriteSteps == null) return;
            var args = rewriteSteps.Any()
                ? rewriteSteps.Select(step => $"{step.Name} ({step.Origin})").ToArray()
                : new string[] { "(none)" };
            this.Logger?.Log(InformationCode.LoadedRewriteSteps, Enumerable.Empty<string>(), messageParam: Formatting.Indent(args).ToArray());
        }


        // routines for loading from and dumping to files

        /// <summary>
        /// Used to load the content of the specified source files from disk. 
        /// Returns a dictionary mapping the file uri to its content. 
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly. 
        /// Prints all loaded files using PrintResolvedFiles.
        /// </summary>
        private ImmutableDictionary<Uri, string> LoadSourceFiles(IEnumerable<string> sources)
        {
            this.CompilationStatus.SourceFileLoading = 0;
            if (sources == null) this.LogAndUpdate(ref this.CompilationStatus.SourceFileLoading, ErrorCode.SourceFilesMissing, Enumerable.Empty<string>());
            void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.SourceFileLoading, ex);
            void onDiagnostic(Diagnostic d) => this.LogAndUpdateLoadDiagnostics(ref this.CompilationStatus.SourceFileLoading, d);
            var sourceFiles = ProjectManager.LoadSourceFiles(sources ?? Enumerable.Empty<string>(), onDiagnostic, onException);
            this.PrintResolvedFiles(sourceFiles.Keys);
            return sourceFiles;
        }

        /// <summary>
        /// Used to load the content of the specified assembly references from disk. 
        /// Returns the loaded content of the references. 
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly. 
        /// Prints all loaded files using PrintResolvedAssemblies.
        /// </summary>
        private References LoadAssemblies(IEnumerable<string> refs, bool ignoreDllResources)
        {
            this.CompilationStatus.ReferenceLoading = 0;
            if (refs == null) this.Logger?.Log(WarningCode.ReferencesSetToNull, Enumerable.Empty<string>());
            void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.ReferenceLoading, ex);
            void onDiagnostic(Diagnostic d) => this.LogAndUpdateLoadDiagnostics(ref this.CompilationStatus.ReferenceLoading, d);
            var headers = ProjectManager.LoadReferencedAssemblies(refs ?? Enumerable.Empty<string>(), onDiagnostic, onException, ignoreDllResources);
            var projId = this.Config.ProjectName == null ? null : Path.ChangeExtension(Path.GetFullPath(this.Config.ProjectNameWithExtension), "qsproj");
            var references = new References(headers, (code, args) => onDiagnostic(Errors.LoadError(code, args, projId)));
            this.PrintResolvedAssemblies(references.Declarations.Keys);
            return references;
        }

        /// <summary>
        /// Writes a binary representation of the built Q# compilation output to the given memory stream. 
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly.
        /// Does *not* close the given memory stream, and
        /// returns true if the serialization has been successfully generated. 
        /// Throws an ArgumentNullException if the given memory stream is null. 
        /// </summary>
        private bool SerializeSyntaxTree(MemoryStream ms)
        {
            if (ms == null) throw new ArgumentNullException(nameof(ms));
            bool ErrorAndReturn()
            {
                this.LogAndUpdate(ref this.CompilationStatus.Serialization, ErrorCode.SerializationFailed, Enumerable.Empty<string>());
                return false;
            }
            this.CompilationStatus.Serialization = 0;
            if (this.CompilationOutput == null) ErrorAndReturn();

            using var writer = new BsonDataWriter(ms) { CloseOutput = false };
            var fromSources = this.CompilationOutput.Namespaces.Select(ns => FilterBySourceFile.Apply(ns, s => s.Value.EndsWith(".qs")));
            var compilation = new QsCompilation(fromSources.ToImmutableArray(), this.CompilationOutput.EntryPoints);
            try { Json.Serializer.Serialize(writer, compilation); }
            catch (Exception ex)
            {
                this.LogAndUpdate(ref this.CompilationStatus.Serialization, ex);
                ErrorAndReturn();
            }
            return true;
        }

        /// <summary>
        /// Backtracks to the beginning of the given memory stream and writes its content to disk,  
        /// generating a suitable bson file in the specified build output folder using the project name as file name.
        /// Generates a file name at random if no project name is specified.  
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly.
        /// Returns the absolute path of the file where the binary representation has been generated. 
        /// Returns null if the binary file could not be generated. 
        /// Does *not* close the given memory stream. 
        /// Throws an ArgumentNullException if the given memory stream is null. 
        /// </summary>
        private string GenerateBinary(MemoryStream serialization)
        {
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));
            this.CompilationStatus.BinaryFormat = 0;

            var projId = NonNullable<string>.New(Path.GetFullPath(this.Config.ProjectNameWithExtension ?? Path.GetRandomFileName()));
            var outFolder = Path.GetFullPath(String.IsNullOrWhiteSpace(this.Config.BuildOutputFolder) ? "." : this.Config.BuildOutputFolder);
            var target = GeneratedFile(projId, outFolder, ".bson", "");

            try
            {
                serialization.Seek(0, SeekOrigin.Begin);
                using (var file = new FileStream(target, FileMode.Create, FileAccess.Write))
                { serialization.WriteTo(file); }
                return target;
            }
            catch (Exception ex)
            {
                this.LogAndUpdate(ref this.CompilationStatus.BinaryFormat, ex);
                this.LogAndUpdate(ref this.CompilationStatus.BinaryFormat, ErrorCode.GeneratingBinaryFailed, Enumerable.Empty<string>());
                return null;
            }
        }

        /// <summary>
        /// Backtracks to the beginning of the given memory stream and, 
        /// assuming the given memory stream contains a serialization of the compiled syntax tree, 
        /// generates a dll containing the compiled binary at the specified dll output path. 
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly.
        /// Returns the absolute path of the file where the dll has been generated. 
        /// Returns null if the dll could not be generated. 
        /// Does *not* close the given memory stream. 
        /// Throws an ArgumentNullException if the given memory stream is null. 
        /// </summary>
        private string GenerateDll(MemoryStream serialization)
        {
            if (serialization == null) throw new ArgumentNullException(nameof(serialization));
            this.CompilationStatus.DllGeneration = 0;

            var fallbackFileName = (this.PathToCompiledBinary ?? this.Config.ProjectNameWithExtension) ?? Path.GetRandomFileName();
            var outputPath = Path.GetFullPath(String.IsNullOrWhiteSpace(this.Config.DllOutputPath) ? fallbackFileName : this.Config.DllOutputPath);
            outputPath = Path.ChangeExtension(outputPath, "dll");

            MetadataReference CreateReference(string file, int id) =>
                MetadataReference.CreateFromFile(file)
                .WithAliases(new string[] { $"{DotnetCoreDll.ReferenceAlias}{id}" }); // referenced Q# dlls are recognized based on this alias 

            // We need to force the inclusion of references despite that we do not include C# code that depends on them. 
            // This is done via generating a certain handle in all dlls built via this compilation loader. 
            // This checks if that handle is available to merely generate a warning if we can't include the reference. 
            bool CanBeIncluded(NonNullable<string> dll)
            {
                try // no need to throw in case this fails - ignore the reference instead
                {
                    using var stream = File.OpenRead(dll.Value);
                    using var assemblyFile = new PEReader(stream);
                    var metadataReader = assemblyFile.GetMetadataReader();
                    return metadataReader.TypeDefinitions
                        .Select(metadataReader.GetTypeDefinition)
                        .Any(t => metadataReader.GetString(t.Namespace) == DotnetCoreDll.MetadataNamespace);
                }
                catch { return false; }
            }

            try
            {
                var referencePaths = GetSourceFiles.Apply(this.CompilationOutput.Namespaces) // we choose to keep only Q# references that have been used
                    .Where(file => file.Value.EndsWith(".dll"));
                var references = referencePaths.Select((dll, id) => (dll, CreateReference(dll.Value, id), CanBeIncluded(dll))).ToImmutableArray();
                var csharpTree = MetadataGeneration.GenerateAssemblyMetadata(references.Where(r => r.Item3).Select(r => r.Item2));
                foreach (var (dropped, _, _) in references.Where(r => !r.Item3))
                {
                    var warning = Warnings.LoadWarning(WarningCode.ReferenceCannotBeIncludedInDll, new[] { dropped.Value }, null);
                    this.LogAndUpdate(ref this.CompilationStatus.DllGeneration, warning);
                }

                var compilation = CodeAnalysis.CSharp.CSharpCompilation.Create(
                    this.Config.ProjectNameWithoutExtension ?? Path.GetFileNameWithoutExtension(outputPath),
                    syntaxTrees: new[] { csharpTree },
                    references: references.Select(r => r.Item2).Append(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)), // if System.Object can't be found a warning is generated
                    options: new CodeAnalysis.CSharp.CSharpCompilationOptions(outputKind: CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
                );

                using var outputStream = File.OpenWrite(outputPath);
                serialization.Seek(0, SeekOrigin.Begin);
                var astResource = new CodeAnalysis.ResourceDescription(DotnetCoreDll.ResourceName, () => serialization, true);
                var result = compilation.Emit(outputStream,
                    options: new CodeAnalysis.Emit.EmitOptions(),
                    manifestResources: new CodeAnalysis.ResourceDescription[] { astResource }
                );

                var errs = result.Diagnostics.Where(d => d.Severity >= CodeAnalysis.DiagnosticSeverity.Error);
                if (errs.Any()) throw new Exception($"error(s) on emitting dll: {Environment.NewLine}{String.Join(Environment.NewLine, errs.Select(d => d.GetMessage()))}");
                return outputPath;
            }
            catch (Exception ex)
            {
                this.LogAndUpdate(ref this.CompilationStatus.DllGeneration, ex);
                this.LogAndUpdate(ref this.CompilationStatus.DllGeneration, ErrorCode.GeneratingDllFailed, Enumerable.Empty<string>());
                return null;
            }
        }

        /// <summary>
        /// Given the path to a Q# binary file, reads the content of that file and returns the corresponding compilation as out parameter. 
        /// Throws the corresponding exception if the given path does not correspond to a suitable binary file.
        /// </summary>
        public static bool ReadBinary(string file, out QsCompilation syntaxTree) =>
            ReadBinary(new MemoryStream(File.ReadAllBytes(Path.GetFullPath(file))), out syntaxTree);

        /// <summary>
        /// Given a stream with the content of a Q# binary file, returns the corresponding compilation as out parameter.
        /// Throws an ArgumentNullException if the given stream is null.
        /// </summary>
        public static bool ReadBinary(Stream stream, out QsCompilation syntaxTree) =>
            AssemblyLoader.LoadSyntaxTree(stream, out syntaxTree);

        /// <summary>
        /// Given a file id assigned by the Q# compiler, computes the corresponding path in the specified output folder. 
        /// Returns the computed absolute path for a file with the specified ending. 
        /// If the content for that file is specified, writes that content to disk. 
        /// Throws an ArgumentException if the given file id is incompatible with and id assigned by the Q# compiler.
        /// Throws the corresponding exception any of the path operations fails or if the writing fails.  
        /// </summary>
        public static string GeneratedFile(NonNullable<string> fileId, string outputFolder, string fileEnding, string content = null)
        {
            if (!CompilationUnitManager.TryGetUri(fileId, out var file))
            { throw new ArgumentException("the given file id is not consistent with and id generated by the Q# compiler"); }
            string FullDirectoryName(string dir) =>
                Path.GetFullPath(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);

            outputFolder = String.IsNullOrWhiteSpace(outputFolder) ? "." : outputFolder;
            var outputUri = new Uri(FullDirectoryName(outputFolder));
            var currentDir = new Uri(FullDirectoryName("."));
            var relFilePath = currentDir.MakeRelativeUri(file);
            var filePath = Uri.UnescapeDataString(new Uri(outputUri, relFilePath).LocalPath);
            var fileDir = filePath.StartsWith(outputUri.LocalPath)
                ? Path.GetDirectoryName(filePath)
                : Path.GetDirectoryName(outputUri.LocalPath);
            var targetFile = Path.GetFullPath(Path.Combine(fileDir, Path.GetFileNameWithoutExtension(filePath) + fileEnding));

            if (content == null) return targetFile;
            if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);
            File.WriteAllText(targetFile, content);
            return targetFile;
        }
    }
}
