// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Quantum.QsCompiler.BuiltInRewriteSteps;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.VisualStudio.LanguageServer.Protocol;

using MetadataReference = Microsoft.CodeAnalysis.MetadataReference;
using OptimizationLevel = Microsoft.CodeAnalysis.OptimizationLevel;

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
        public static Func<string, Assembly>? LoadAssembly { get; set; }

        /// <summary>
        /// may be specified via configuration (or project) file in the future
        /// </summary>
        public class Configuration
        {
            /// <summary>
            /// The name of the project. Used as assembly name in the generated dll.
            /// The name of the project with a suitable extension will also be used as the name of the generated binary file.
            /// </summary>
            public string? ProjectName { get; set; }

            /// <summary>
            /// If set to true, forces all rewrite steps to execute, regardless of whether their precondition was satisfied.
            /// If the precondition of a step is not satisfied, the transformation is executed but the output will be ignored,
            /// and an error is generated, indicating a compilation failure.
            /// </summary>
            public bool ForceRewriteStepExecution { get; set; }

            /// <summary>
            /// If set to true, the syntax tree rewrite step that replaces all generation directives
            /// for all functor specializations is executed during compilation.
            /// </summary>
            public bool GenerateFunctorSupport { get; set; }

            /// <summary>
            /// Unless this is set to true, the syntax tree rewrite step that inlines conjugations is executed during compilation.
            /// </summary>
            public bool SkipConjugationInlining { get; set; }

            /// <summary>
            /// Unless this is set to true, all unused callables are removed from the syntax tree.
            /// </summary>
            public bool SkipSyntaxTreeTrimming { get; set; }

            /// <summary>
            /// If set to true, the compiler attempts to pre-evaluate the built compilation as much as possible.
            /// This is an experimental feature that will change over time.
            /// </summary>
            public bool AttemptFullPreEvaluation { get; set; }

            /// <summary>
            /// Specifies the capabilities of the runtime.
            /// The specified capabilities determine what QIR profile to compile to.
            /// </summary>
            public RuntimeCapability? RuntimeCapability { get; set; }

            /// <summary>
            /// Specifies whether the project to build is a Q# command line application.
            /// If set to true, a warning will be raised if no entry point is defined.
            /// If set to false, then defined entry points will be ignored and a warning will be raised.
            /// </summary>
            public bool IsExecutable { get; set; }

            /// <summary>
            /// Unless this is set to true, all usages of type-parameterized callables are replaced with
            /// the concrete callable instantiation if an entry point is specified for the compilation.
            /// Removes all type-parameterizations in the syntax tree.
            /// </summary>
            public bool SkipMonomorphization { get; set; }

            /// <summary>
            /// Indicates whether the compiler will remove lambda expressions and replace them with calls to generated callables.
            /// </summary>
            public bool LiftLambdaExpressions { get; set; } = true;

            /// <summary>
            /// If the output folder is not null,
            /// documentation is generated in the specified folder based on doc comments in the source code.
            /// </summary>
            public string? DocumentationOutputFolder { get; set; }

            /// <summary>
            /// Directory where the compiled binaries will be generated.
            /// No binaries will be written to disk unless this path is specified and valid.
            /// </summary>
            public string? BuildOutputFolder { get; set; }

            /// <summary>
            /// Output path for the dll containing the compiled binaries.
            /// No dll will be generated unless this path is specified and valid.
            /// </summary>
            public string? DllOutputPath { get; set; }

            /// <summary>
            /// If set to true, then referenced dlls will be loaded purely based on attributes in the contained C# code.
            /// Any Q# resources will be ignored.
            /// </summary>
            public bool LoadReferencesBasedOnGeneratedCsharp { get; set; }

            /// <summary>
            /// If set to true, then public types and callables declared in referenced assemblies
            /// are exposed via their test name defined by the corresponding attribute.
            /// </summary>
            public bool ExposeReferencesViaTestNames { get; set; }

            /// <summary>
            /// Contains a sequence of tuples with the path to a dotnet dll containing one or more rewrite steps
            /// (i.e. classes implementing IRewriteStep) and the corresponding output folder.
            /// The contained rewrite steps will be executed in the defined order and priority at the end of the compilation.
            /// </summary>
            public IEnumerable<(string, string?)> RewriteStepAssemblies { get; set; } =
                Enumerable.Empty<(string, string?)>();

            /// <summary>
            /// Contains a sequence of tuples with the types (classes implementing IRewriteStep) and the corresponding output folder.
            /// The contained rewrite steps will be executed in the defined order and priority at the end of the compilation.
            /// </summary>
            public IEnumerable<(Type, string?)> RewriteStepTypes { get; set; } = Enumerable.Empty<(Type, string?)>();

            /// <summary>
            /// Contains a sequence of tuples with the objects (instances of IRewriteStep) and the corresponding output folder.
            /// The contained rewrite steps will be executed in the defined order and priority at the end of the compilation.
            /// </summary>
            public IEnumerable<(IRewriteStep, string?)> RewriteStepInstances { get; set; } =
                Enumerable.Empty<(IRewriteStep, string?)>();

            /// <summary>
            /// If set to true, the post-condition for loaded rewrite steps is checked if the corresponding verification is implemented.
            /// Otherwise post-condition verifications are skipped.
            /// </summary>
            public bool EnableAdditionalChecks { get; set; }

            /// <summary>
            /// Handle to pass arbitrary constants with which to populate the corresponding dictionary for loaded rewrite steps.
            /// These values will take precedence over any already existing values that the default constructor sets.
            /// However, the compiler may overwrite the assembly constants defined for the Q# compilation unit in the dictionary of the loaded step.
            /// The given dictionary in this configuration is left unchanged in any case.
            /// </summary>
            public IReadOnlyDictionary<string, string>? AssemblyConstants { get; set; }

            /// <summary>
            /// Paths to the assemblies that contains a syntax tree with target specific implementations for certain functions and operations.
            /// The functions and operations defined in these assemblies replace the ones declared within the compilation unit.
            /// If no paths are specified here or the sequence is null then this compilation step is omitted.
            /// </summary>
            public IEnumerable<string>? TargetPackageAssemblies { get; set; }

            /// <summary>
            /// Indicates whether the necessary compiler passes are executed for the compilation to be compatible with QIR generation.
            /// </summary>
            public bool PrepareQirGeneration { get; set; }

            /// <summary>
            /// Indicates whether a serialization of the syntax tree needs to be generated.
            /// This is the case if either the build output folder is specified or the dll output path is specified.
            /// </summary>
            internal bool SerializeSyntaxTree =>
                this.BuildOutputFolder != null || this.DllOutputPath != null;

            /// <summary>
            /// Indicates whether the compilation needs to be monomorphized.
            /// This value is never true if SkipMonomorphization is specified.
            /// </summary>
            internal bool Monomorphize =>
                (this.IsExecutable || this.PrepareQirGeneration) && !this.SkipMonomorphization;

            /// <summary>
            /// Indicates whether the compiler will remove if-statements and replace them with calls to appropriate intrinsic operations.
            /// </summary>
            internal bool ConvertClassicalControl =>
                this.RuntimeCapability?.ResultOpacity.Equals(ResultOpacityModule.Controlled) ?? true;

            /// <summary>
            /// Indicates whether any paths to assemblies have been specified that may contain target specific decompositions.
            /// </summary>
            internal bool LoadTargetSpecificDecompositions =>
                this.TargetPackageAssemblies != null && this.TargetPackageAssemblies.Any();

            /// <summary>
            /// If the ProjectName does not have an ending "proj", appends a .qsproj ending to the project name.
            /// Returns null if the project name is null.
            /// </summary>
            internal string? ProjectNameWithExtension =>
                this.ProjectName == null ? null :
                this.ProjectName.EndsWith("proj") ? this.ProjectName :
                $"{this.ProjectName}.qsproj";

            /// <summary>
            /// If the ProjectName does have an extension ending with "proj", returns the project name without that extension.
            /// Returns null if the project name is null.
            /// </summary>
            internal string? ProjectNameWithoutPathOrExtension =>
                this.ProjectName == null ? null :
                Path.GetExtension(this.ProjectName).EndsWith("proj") ? Path.GetFileNameWithoutExtension(this.ProjectName) :
                Path.GetFileName(this.ProjectName);
        }

        /// <summary>
        /// used to indicate the status of individual compilation steps
        /// </summary>
        public enum Status
        {
            /// <summary>
            /// Indicates that a compilation step has not been executed.
            /// </summary>
            NotRun = -1,

            /// <summary>
            /// Indicates that a compilation step successfully executed.
            /// </summary>
            Succeeded = 0,

            /// <summary>
            /// Indicates that a compilation step executed but failed.
            /// </summary>
            Failed = 1,
        }

        [SuppressMessage(
            "StyleCop.CSharp.MaintainabilityRules",
            "SA1401:FieldsMustBePrivate",
            Justification = "Fields are passed by reference.")]
        private class ExecutionStatus
        {
            internal Status SourceFileLoading = Status.NotRun;
            internal Status ReferenceLoading = Status.NotRun;
            internal Status PluginLoading = Status.NotRun;
            internal Status Validation = Status.NotRun;
            internal Status TargetSpecificReplacements = Status.NotRun;
            internal Status FunctorSupport = Status.NotRun;
            internal Status PreEvaluation = Status.NotRun;
            internal Status ConjugationInlining = Status.NotRun;
            internal Status TreeTrimming = Status.NotRun;
            internal Status LiftLambdaExpressions = Status.NotRun;
            internal Status ConvertClassicalControl = Status.NotRun;
            internal Status Monomorphization = Status.NotRun;
            internal Status Documentation = Status.NotRun;
            internal Status Serialization = Status.NotRun;
            internal Status BinaryFormat = Status.NotRun;
            internal Status DllGeneration = Status.NotRun;
            internal Status CapabilityInference = Status.NotRun;
            internal Status[] LoadedRewriteSteps;

            internal ExecutionStatus(IEnumerable<IRewriteStep> externalRewriteSteps) =>
                this.LoadedRewriteSteps = externalRewriteSteps.Select(_ => Status.NotRun).ToArray();

            private bool WasSuccessful(bool run, Status code) =>
                (run && code == Status.Succeeded) || (!run && code == Status.NotRun);

            internal bool Success(Configuration options) =>
                this.SourceFileLoading <= 0 &&
                this.ReferenceLoading <= 0 &&
                this.WasSuccessful(true, this.Validation) &&
                this.WasSuccessful(true, this.PluginLoading) &&
                this.WasSuccessful(options.IsExecutable && !options.SkipSyntaxTreeTrimming, this.TreeTrimming) &&
                this.WasSuccessful(options.GenerateFunctorSupport, this.FunctorSupport) &&
                this.WasSuccessful(!options.SkipConjugationInlining, this.ConjugationInlining) &&
                this.WasSuccessful(options.AttemptFullPreEvaluation, this.PreEvaluation) &&
                this.WasSuccessful(options.LoadTargetSpecificDecompositions, this.TargetSpecificReplacements) &&
                this.WasSuccessful(options.ConvertClassicalControl, this.ConvertClassicalControl) &&
                this.WasSuccessful(options.Monomorphize, this.Monomorphization) &&
                this.WasSuccessful(!options.IsExecutable, this.CapabilityInference) &&
                this.WasSuccessful(options.SerializeSyntaxTree, this.Serialization) &&
                this.WasSuccessful(options.BuildOutputFolder != null, this.BinaryFormat) &&
                this.WasSuccessful(options.DllOutputPath != null, this.DllGeneration) &&
                this.LoadedRewriteSteps.All(status => this.WasSuccessful(true, status));
        }

        /// <summary>
        /// Indicates whether all source files were loaded successfully.
        /// Source file loading may not be executed if the content was preloaded using methods outside this class.
        /// </summary>
        public Status SourceFileLoading => this.compilationStatus.SourceFileLoading;

        /// <summary>
        /// Indicates whether all references were loaded successfully.
        /// The loading may not be executed if all references were preloaded using methods outside this class.
        /// </summary>
        public Status ReferenceLoading => this.compilationStatus.ReferenceLoading;

        /// <summary>
        /// Indicates whether all external dlls specifying e.g. rewrite steps
        /// to perform as part of the compilation have been loaded successfully.
        /// The status indicates a successful execution if no such external dlls have been specified.
        /// </summary>
        public Status PluginLoading => this.compilationStatus.PluginLoading;

        /// <summary>
        /// Indicates whether the compilation unit passed the compiler validation
        /// that is executed before invoking further rewrite and/or generation steps.
        /// </summary>
        public Status Validation => this.compilationStatus.Validation;

        /// <summary>
        /// Indicates whether all lambda expressions were replaced successfully with
        /// calls to generated callables.
        /// </summary>
        public Status LiftLambdaExpressions => this.compilationStatus.LiftLambdaExpressions;

        /// <summary>
        /// Indicates whether any target-specific compilation steps executed successfully.
        /// This includes the step to convert control flow statements when needed.
        /// </summary>
        public Status TargetSpecificCompilation => this.compilationStatus.ConvertClassicalControl;

        /// <summary>
        /// Indicates whether target specific implementations for functions and operations
        /// have been used to replace the ones declared within the compilation unit.
        /// This step is only executed if the specified configuration contains the path to the target package.
        /// </summary>
        public Status TargetSpecificReplacements => this.compilationStatus.TargetSpecificReplacements;

        /// <summary>
        /// Indicates whether all specializations were generated successfully.
        /// This rewrite step is only executed if the corresponding configuration is specified.
        /// </summary>
        public Status FunctorSupport => this.compilationStatus.FunctorSupport;

        /// <summary>
        /// Indicates whether the pre-evaluation step executed successfully.
        /// This rewrite step is only executed if the corresponding configuration is specified.
        /// </summary>
        public Status PreEvaluation => this.compilationStatus.PreEvaluation;

        /// <summary>
        /// Indicates whether all the type-parameterized callables were resolved to concrete callables.
        /// This rewrite step is only executed if the corresponding configuration is specified.
        /// </summary>
        public Status Monomorphization => this.compilationStatus.Monomorphization;

        /// <summary>
        /// Indicates whether the inference of required runtime capabilities for execution completed successfully.
        /// This rewrite step is only executed when compiling a library.
        /// </summary>
        public Status CapabilityInference => this.compilationStatus.CapabilityInference;

        /// <summary>
        /// Indicates whether documentation for the compilation was generated successfully.
        /// This step is only executed if the corresponding configuration is specified.
        /// </summary>
        public Status Documentation => this.compilationStatus.Documentation;

        /// <summary>
        /// Indicates whether the built compilation could be serialized successfully.
        /// This step is only executed if either the binary representation or a dll is emitted.
        /// </summary>
        public Status Serialization => this.compilationStatus.Serialization;

        /// <summary>
        /// Indicates whether a binary representation for the generated syntax tree has been generated successfully.
        /// This step is only executed if the corresponding configuration is specified.
        /// </summary>
        public Status BinaryFormat => this.compilationStatus.BinaryFormat;

        /// <summary>
        /// Indicates whether a dll containing the compiled binary has been generated successfully.
        /// This step is only executed if the corresponding configuration is specified.
        /// </summary>
        public Status DllGeneration => this.compilationStatus.DllGeneration;

        /// <summary>
        /// Indicates whether all rewrite steps with the given name and loaded from the given source executed successfully.
        /// The source, if specified, is the path to the dll in which the step is specified.
        /// Returns a status NotRun if no such step was found or executed.
        /// Execution is considered successful if the precondition and transformation (if any) returned true.
        /// </summary>
        public Status LoadedRewriteStep(string name, string? source = null)
        {
            var uri = string.IsNullOrWhiteSpace(source) ? null : new Uri(Path.GetFullPath(source));
            bool MatchesQuery(int index) => this.externalRewriteSteps[index].Name == name && (source == null || this.externalRewriteSteps[index].Origin == uri);
            var statuses = this.compilationStatus.LoadedRewriteSteps.Where((s, i) => MatchesQuery(i)).ToArray();
            return statuses.All(s => s == Status.Succeeded) ? Status.Succeeded : statuses.Any(s => s == Status.Failed) ? Status.Failed : Status.NotRun;
        }

        /// <summary>
        /// Indicates the overall status of all rewrite step from external dlls.
        /// The status is indicated as success if none of these steps failed.
        /// </summary>
        public Status AllLoadedRewriteSteps => this.compilationStatus.LoadedRewriteSteps.Any(s => s == Status.Failed) ? Status.Failed : Status.Succeeded;

        /// <summary>
        /// Indicates the overall success of all compilation steps.
        /// The compilation is indicated as having been successful if all steps that were configured to execute completed successfully.
        /// </summary>
        public bool Success => this.compilationStatus.Success(this.config);

        /// <summary>
        /// Logger used to log all diagnostic events during compilation.
        /// </summary>
        private readonly ILogger? logger;

        /// <summary>
        /// Configuration specifying the compilation steps to execute.
        /// </summary>
        private readonly Configuration config;

        /// <summary>
        /// Used to track the status of individual compilation steps.
        /// </summary>
        private readonly ExecutionStatus compilationStatus;

        /// <summary>
        /// Contains all loaded rewrite steps found in the specified plugin DLLs,
        /// the passed in types implementing IRewriteStep and instances of IRewriteStep,
        /// where configurable properties such as the output folder have already been initialized to suitable values.
        /// </summary>
        private readonly ImmutableArray<LoadedStep> externalRewriteSteps;

        /// <summary>
        /// Contains all diagnostics generated upon source file and reference loading.
        /// All other diagnostics can be accessed via the VerifiedCompilation.
        /// </summary>
        public ImmutableArray<Diagnostic> LoadDiagnostics { get; set; }

        /// <summary>
        /// Contains the initial compilation built by the compilation unit manager after verification.
        /// </summary>
        public CompilationUnitManager.Compilation? VerifiedCompilation { get; }

        /// <summary>
        /// Contains the built compilation including the syntax tree after executing all configured rewrite steps.
        /// </summary>
        public QsCompilation? CompilationOutput { get; private set; }

        /// <summary>
        /// Contains the absolute path where the binary representation of the generated syntax tree has been written to disk.
        /// </summary>
        public string? PathToCompiledBinary { get; private set; }

        /// <summary>
        /// Contains the absolute path where the generated dll containing the compiled binary has been written to disk.
        /// </summary>
        public string? DllOutputPath { get; private set; }

        /// <summary>
        /// Contains the full Q# syntax tree after executing all configured rewrite steps, including the content of loaded references.
        /// </summary>
        public IEnumerable<QsNamespace>? GeneratedSyntaxTree =>
            this.CompilationOutput?.Namespaces;

        /// <summary>
        /// Contains the Uri and names of all rewrite steps loaded from the specified dlls
        /// in the order in which they are executed.
        /// </summary>
        public ImmutableArray<(Uri, string)> LoadedRewriteSteps =>
            this.externalRewriteSteps.Select(step => (step.Origin, step.Name)).ToImmutableArray();

        /// <summary>
        /// Builds the compilation for the source files and references loaded by the given loaders,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events.
        /// </summary>
        /// <remarks>This method waits for <see cref="System.Threading.Tasks.Task"/>s to complete and may deadlock if invoked through a <see cref="System.Threading.Tasks.Task"/>.</remarks>
        public CompilationLoader(SourceLoader loadSources, ReferenceLoader loadReferences, Configuration? options = null, ILogger? logger = null)
        {
            PerformanceTracking.TaskStart(PerformanceTracking.Task.OverallCompilation);

            // loading the content to compiler
            this.logger = logger;
            this.LoadDiagnostics = ImmutableArray<Diagnostic>.Empty;
            this.config = options ?? new Configuration();

            // When loading references is done through the generated C# a Bond deserializer is not needed.
            if (!this.config.LoadReferencesBasedOnGeneratedCsharp)
            {
                BondSchemas.Protocols.InitializeDeserializer();
            }

            // When the syntax tree is not serialized a Bond serializer is not needed.
            if (this.config.SerializeSyntaxTree)
            {
                BondSchemas.Protocols.InitializeSerializer();
            }

            Status rewriteStepLoading = Status.Succeeded;
            this.externalRewriteSteps = ExternalRewriteStepsManager.Load(this.config, d => this.LogAndUpdateLoadDiagnostics(ref rewriteStepLoading, d), ex => this.LogAndUpdate(ref rewriteStepLoading, ex));
            this.PrintLoadedRewriteSteps(this.externalRewriteSteps);
            this.compilationStatus = new ExecutionStatus(this.externalRewriteSteps);
            this.compilationStatus.PluginLoading = rewriteStepLoading;

            PerformanceTracking.TaskStart(PerformanceTracking.Task.SourcesLoading);
            var sourceFiles = loadSources(this.LoadSourceFiles);
            PerformanceTracking.TaskEnd(PerformanceTracking.Task.SourcesLoading);
            PerformanceTracking.TaskStart(PerformanceTracking.Task.ReferenceLoading);
            var references = loadReferences(
                refs => this.LoadAssemblies(
                    refs,
                    loadTestNames: this.config.ExposeReferencesViaTestNames,
                    ignoreDllResources: this.config.LoadReferencesBasedOnGeneratedCsharp));
            PerformanceTracking.TaskEnd(PerformanceTracking.Task.ReferenceLoading);

            // building the compilation
            PerformanceTracking.TaskStart(PerformanceTracking.Task.Build);
            this.compilationStatus.Validation = Status.Succeeded;
            var files = CompilationUnitManager.InitializeFileManagers(sourceFiles, null, this.OnCompilerException); // do *not* live track (i.e. use publishing) here!

            var processorArchitecture = this.config.AssemblyConstants?.GetValueOrDefault(AssemblyConstants.ProcessorArchitecture);
            var buildProperties = ImmutableDictionary.CreateBuilder<string, string?>();
            buildProperties.Add(MSBuildProperties.ResolvedRuntimeCapabilities, this.config.RuntimeCapability?.Name);
            buildProperties.Add(MSBuildProperties.ResolvedQsharpOutputType, this.config.IsExecutable ? AssemblyConstants.QsharpExe : AssemblyConstants.QsharpLibrary);
            buildProperties.Add(MSBuildProperties.ResolvedProcessorArchitecture, processorArchitecture);

            var compilationManager = new CompilationUnitManager(
                new ProjectProperties(buildProperties),
                this.OnCompilerException);
            compilationManager.UpdateReferencesAsync(references);
            compilationManager.AddOrUpdateSourceFilesAsync(files);
            this.VerifiedCompilation = compilationManager.Build();
            this.CompilationOutput = this.VerifiedCompilation?.BuiltCompilation;
            compilationManager.Dispose();

            foreach (var diag in this.VerifiedCompilation?.Diagnostics() ?? Enumerable.Empty<Diagnostic>())
            {
                this.LogAndUpdate(ref this.compilationStatus.Validation, diag);
            }

            if (this.config.IsExecutable && this.CompilationOutput?.EntryPoints.Length == 0)
            {
                if (this.config.RuntimeCapability?.CompareTo(RuntimeCapabilityModule.Top) < 0)
                {
                    this.LogAndUpdate(ref this.compilationStatus.Validation, ErrorCode.MissingEntryPoint);
                }
                else
                {
                    this.LogAndUpdate(ref this.compilationStatus.Validation, WarningCode.MissingEntryPoint);
                }
            }

            if (!Uri.TryCreate(Assembly.GetExecutingAssembly().CodeBase, UriKind.Absolute, out Uri thisDllUri))
            {
                thisDllUri = new Uri(Path.GetFullPath(".", "CompilationLoader.cs"));
            }

            if (this.Validation == Status.Succeeded && this.config.LoadTargetSpecificDecompositions)
            {
                PerformanceTracking.TaskStart(PerformanceTracking.Task.ReplaceTargetSpecificImplementations);
                this.ReplaceTargetSpecificImplementations(
                    this.config.TargetPackageAssemblies ?? Enumerable.Empty<string>(),
                    thisDllUri,
                    references.Declarations.Count);
                PerformanceTracking.TaskEnd(PerformanceTracking.Task.ReplaceTargetSpecificImplementations);
            }

            PerformanceTracking.TaskEnd(PerformanceTracking.Task.Build);

            if (this.Validation == Status.Succeeded)
            {
                this.RunRewriteSteps(thisDllUri);
                this.GenerateOutput();
            }

            PerformanceTracking.TaskEnd(PerformanceTracking.Task.OverallCompilation);
        }

        /// <summary>
        /// Builds the compilation of the specified source files and references,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events.
        /// </summary>
        public CompilationLoader(IEnumerable<string> sources, IEnumerable<string> references, Configuration? options = null, ILogger? logger = null)
            : this(load => load(sources), load => load(references), options, logger)
        {
        }

        /// <summary>
        /// Builds the compilation of the specified source files and the loaded references returned by the given loader,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events.
        /// </summary>
        public CompilationLoader(IEnumerable<string> sources, ReferenceLoader loadReferences, Configuration? options = null, ILogger? logger = null)
            : this(load => load(sources), loadReferences, options, logger)
        {
        }

        /// <summary>
        /// Builds the compilation of the content returned by the given loader and the specified references,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events.
        /// </summary>
        public CompilationLoader(SourceLoader loadSources, IEnumerable<string> references, Configuration? options = null, ILogger? logger = null)
            : this(loadSources, load => load(references), options, logger)
        {
        }

        private IEnumerable<(LoadedStep, Action<Status>)> InternalRewriteSteps(Uri executingAssembly)
        {
            // TODO: The dependencies for rewrite steps should be declared as part of IRewriteStep interface, and we
            // should query those here.
#pragma warning disable CS0618 // Type or member is obsolete
            var dependencies = BuiltIn.AllBuiltIns.Select(b => b.FullName);
#pragma warning restore CS0618 // Type or member is obsolete

            var status = this.compilationStatus;
            var steps = new (IRewriteStep Step, bool Enabled, Action<Status> SetStatus)[]
            {
                // TODO: It would be nicer to trim unused intrinsics. Currently, this is not possible due to how the
                // old setup of the C# runtime works. With the new setup (interface-based approach for target
                // packages), it is possible to trim unused intrinsics.
                (
                    new SyntaxTreeTrimming(keepAllIntrinsics: true, dependencies),
                    this.config.IsExecutable && !this.config.SkipSyntaxTreeTrimming,
                    s => status.TreeTrimming = s),

                (new LiftLambdas(), this.config.LiftLambdaExpressions, s => status.LiftLambdaExpressions = s),
                (new ClassicallyControlled(), this.config.ConvertClassicalControl, s => status.ConvertClassicalControl = s),
                (new FunctorGeneration(), this.config.GenerateFunctorSupport, s => status.FunctorSupport = s),
                (new ConjugationInlining(), !this.config.SkipConjugationInlining, s => status.ConjugationInlining = s),
                (new FullPreEvaluation(), this.config.AttemptFullPreEvaluation, s => status.PreEvaluation = s),
                (new Monomorphization(monomorphizeIntrinsics: false), this.config.Monomorphize, s => status.Monomorphization = s),
                (new CapabilityInference(), !this.config.IsExecutable, s => status.CapabilityInference = s),
            };

            return from step in steps
                where step.Enabled
                select (new LoadedStep(step.Step, typeof(IRewriteStep), executingAssembly), step.SetStatus);
        }

        private void RunRewriteSteps(Uri executingAssembly)
        {
            PerformanceTracking.TaskStart(PerformanceTracking.Task.RewriteSteps);
            this.config.PrepareQirGeneration = this.config.PrepareQirGeneration || this.externalRewriteSteps.Any(step => step.Name == "QIR Generation");

            var externalSteps = this.externalRewriteSteps.Select((step, index) =>
                (step, new Action<Status>(status => this.compilationStatus.LoadedRewriteSteps[index] = status)));

            var steps = this.InternalRewriteSteps(executingAssembly)
                .Concat(externalSteps)
                .OrderByDescending(step => step.Item1.Priority);

            foreach (var (step, setStatus) in steps)
            {
                PerformanceTracking.TaskStart(PerformanceTracking.Task.SingleRewriteStep, step.Name);
                setStatus(this.RunRewriteStep(step));
                PerformanceTracking.TaskEnd(PerformanceTracking.Task.SingleRewriteStep, step.Name);
            }

            PerformanceTracking.TaskEnd(PerformanceTracking.Task.RewriteSteps);
        }

        private void GenerateOutput()
        {
            PerformanceTracking.TaskStart(PerformanceTracking.Task.OutputGeneration);
            using var stream = new MemoryStream();

            PerformanceTracking.TaskStart(PerformanceTracking.Task.SyntaxTreeSerialization);
            var serialized = this.config.SerializeSyntaxTree && this.WriteSyntaxTreeSerialization(stream);
            PerformanceTracking.TaskEnd(PerformanceTracking.Task.SyntaxTreeSerialization);

            if (serialized)
            {
                if (this.config.BuildOutputFolder is not null)
                {
                    PerformanceTracking.TaskStart(PerformanceTracking.Task.BinaryGeneration);
                    this.PathToCompiledBinary = this.GenerateBinary(stream);
                    PerformanceTracking.TaskEnd(PerformanceTracking.Task.BinaryGeneration);
                }

                if (this.config.DllOutputPath is not null)
                {
                    PerformanceTracking.TaskStart(PerformanceTracking.Task.DllGeneration);
                    this.DllOutputPath = this.GenerateDll(stream);
                    PerformanceTracking.TaskEnd(PerformanceTracking.Task.DllGeneration);
                }
            }

            PerformanceTracking.TaskEnd(PerformanceTracking.Task.OutputGeneration);
        }

        // private routines used for logging and status updates

        /// <summary>
        /// Logs the given diagnostic and updates the status passed as reference accordingly.
        /// </summary>
        private void LogAndUpdate(ref Status current, Diagnostic d)
        {
            this.logger?.Log(d);
            if (d.IsError())
            {
                current = Status.Failed;
            }
        }

        /// <summary>
        /// Logs the given exception and updates the status passed as reference accordingly.
        /// </summary>
        private void LogAndUpdate(ref Status current, Exception ex)
        {
            this.logger?.Log(ex);
            current = Status.Failed;
        }

        /// <summary>
        /// Logs an error with the given error code and message parameters, and updates the status passed as reference accordingly.
        /// </summary>
        private void LogAndUpdate(ref Status current, ErrorCode code, params string[] args)
        {
            this.logger?.Log(code, args);
            current = Status.Failed;
        }

        /// <summary>
        /// Logs an error with the given warning code and message parameters, and updates the status passed as reference accordingly.
        /// </summary>
        private void LogAndUpdate(ref Status current, WarningCode code, params string[] args)
        {
            this.logger?.Log(code, args);
        }

        /// <summary>
        /// Logs the given diagnostic and updates the status passed as reference accordingly.
        /// Adds the given diagnostic to the tracked load diagnostics.
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
            this.LogAndUpdate(ref this.compilationStatus.Validation, ErrorCode.UnexpectedCompilerException);
            this.LogAndUpdate(ref this.compilationStatus.Validation, ex);
        }

        /// <summary>
        /// Logs the names of the given source files as Information.
        /// Does nothing if the given argument is null.
        /// </summary>
        private void PrintResolvedFiles(IEnumerable<Uri> sourceFiles)
        {
            var args = sourceFiles.Any()
                ? sourceFiles.Select(f => f.LocalPath).ToArray()
                : new string[] { "(none)" };
            this.logger?.Log(InformationCode.CompilingWithSourceFiles, Enumerable.Empty<string>(), messageParam: Formatting.Indent(args).ToArray());
        }

        /// <summary>
        /// Logs the names of the given assemblies as Information.
        /// Does nothing if the given argument is null.
        /// </summary>
        private void PrintResolvedAssemblies(IEnumerable<string> assemblies)
        {
            if (assemblies == null)
            {
                return;
            }

            var args = assemblies.Any()
                ? assemblies.ToArray()
                : new string[] { "(none)" };
            this.logger?.Log(InformationCode.CompilingWithAssemblies, Enumerable.Empty<string>(), messageParam: Formatting.Indent(args).ToArray());
        }

        /// <summary>
        /// Logs the names and origins of the given rewrite steps as Information.
        /// Does nothing if the given argument is null.
        /// </summary>
        private void PrintLoadedRewriteSteps(IEnumerable<LoadedStep> rewriteSteps)
        {
            if (rewriteSteps == null)
            {
                return;
            }

            var args = rewriteSteps.Any()
                ? rewriteSteps.Select(step => $"{step.Name} ({step.Origin})").ToArray()
                : new string[] { "(none)" };
            this.logger?.Log(InformationCode.LoadedRewriteSteps, Enumerable.Empty<string>(), messageParam: Formatting.Indent(args).ToArray());
        }

        // private helper methods used during construction

        /// <summary>
        /// Attempts to load the target package assemblies with the given paths, logging diagnostics
        /// when a path is invalid, or loading fails. Logs suitable diagnostics if the loaded dlls
        /// contains conflicting declarations. Updates the compilation status accordingly.
        /// Executes the transformation to replace target specific implementations as atomic rewrite step.
        /// </summary>
        private void ReplaceTargetSpecificImplementations(IEnumerable<string> paths, Uri rewriteStepOrigin, int nrReferences)
        {
            void LogError(ErrorCode errCode, params string[] args) => this.LogAndUpdate(ref this.compilationStatus.TargetSpecificReplacements, errCode, args);
            void LogException(Exception ex) => this.LogAndUpdate(ref this.compilationStatus.TargetSpecificReplacements, ex);

            (string, ImmutableArray<QsNamespace>)? LoadReferences(string path)
            {
                try
                {
                    var targetDll = Path.GetFullPath(path);
                    if (AssemblyLoader.LoadReferencedAssembly(targetDll, out var loaded, LogException))
                    {
                        return (path, loaded.Namespaces);
                    }

                    LogError(ErrorCode.FailedToLoadTargetSpecificDecompositions, targetDll);
                    return null;
                }
                catch (Exception ex)
                {
                    LogError(ErrorCode.InvalidPathToTargetSpecificDecompositions, path);
                    LogException(ex);
                    return null;
                }
            }

            var natives = paths.SelectNotNull(LoadReferences).ToArray();
            var combinedSuccessfully = References.CombineSyntaxTrees(out var replacements, additionalAssemblies: nrReferences, onError: LogError, natives);
            if (!combinedSuccessfully)
            {
                LogError(ErrorCode.ConflictsInTargetSpecificDecompositions);
            }

            var targetSpecificDecompositions = new QsCompilation(replacements, ImmutableArray<QsQualifiedName>.Empty);
            var step = new LoadedStep(new IntrinsicResolution(targetSpecificDecompositions), typeof(IRewriteStep), rewriteStepOrigin);
            this.compilationStatus.TargetSpecificReplacements = this.RunRewriteStep(step);
        }

        /// <summary>
        /// Runs the given rewrite step on the current compilation. Catches and logs any thrown exception.
        /// </summary>
        /// <returns>The status of the rewrite step.</returns>
        private Status RunRewriteStep(LoadedStep rewriteStep)
        {
            if (this.CompilationOutput is not { } compilation)
            {
                return Status.NotRun;
            }

            string? GetDiagnosticsCode(DiagnosticSeverity severity) =>
                rewriteStep.Name == "CSharpGeneration" && severity == DiagnosticSeverity.Error ? Errors.Code(ErrorCode.CsharpGenerationGeneratedError) :
                rewriteStep.Name == "CSharpGeneration" && severity == DiagnosticSeverity.Warning ? Warnings.Code(WarningCode.CsharpGenerationGeneratedWarning) :
                rewriteStep.Name == "CSharpGeneration" && severity == DiagnosticSeverity.Information ? Informations.Code(InformationCode.CsharpGenerationGeneratedInfo) :
                rewriteStep.Name == "QIR Generation" && severity == DiagnosticSeverity.Error ? Errors.Code(ErrorCode.QirEmissionGeneratedError) :
                rewriteStep.Name == "QIR Generation" && severity == DiagnosticSeverity.Warning ? Warnings.Code(WarningCode.QirEmissionGeneratedWarning) :
                rewriteStep.Name == "QIR Generation" && severity == DiagnosticSeverity.Information ? Informations.Code(InformationCode.QirEmissionGeneratedInfo) :
                null;

            var messageSource = ProjectManager.MessageSource(rewriteStep.Origin);
            void LogDiagnostics(ref Status status)
            {
                try
                {
                    var steps = rewriteStep.GeneratedDiagnostics ?? ImmutableArray<IRewriteStep.Diagnostic>.Empty;
                    foreach (var diagnostic in steps)
                    {
                        this.LogAndUpdate(ref status, LoadedStep.ConvertDiagnostic(rewriteStep.Name, diagnostic, GetDiagnosticsCode));
                    }
                }
                catch
                {
                    this.LogAndUpdate(ref status, WarningCode.RewriteStepDiagnosticsGenerationFailed, rewriteStep.Name, messageSource);
                }
            }

            QsCompilation? newCompilation = null;
            var status = Status.Succeeded;
            try
            {
                var preconditionFailed = rewriteStep.ImplementsPreconditionVerification && !rewriteStep.PreconditionVerification(compilation);
                if (preconditionFailed)
                {
                    LogDiagnostics(ref status);
                    if (this.config.ForceRewriteStepExecution)
                    {
                        this.LogAndUpdate(ref status, ErrorCode.PreconditionVerificationFailed, rewriteStep.Name, messageSource);
                    }
                    else
                    {
                        this.LogAndUpdate(ref status, WarningCode.PreconditionVerificationFailed, rewriteStep.Name, messageSource);
                        return status;
                    }
                }

                var transformationFailed = rewriteStep.ImplementsTransformation && (!rewriteStep.Transformation(compilation, out newCompilation) || newCompilation is null);
                var postconditionFailed = this.config.EnableAdditionalChecks && rewriteStep.ImplementsPostconditionVerification && !rewriteStep.PostconditionVerification(newCompilation);
                LogDiagnostics(ref status);

                if (transformationFailed)
                {
                    this.LogAndUpdate(ref status, ErrorCode.RewriteStepExecutionFailed, new[] { rewriteStep.Name, messageSource });
                }

                if (postconditionFailed)
                {
                    this.LogAndUpdate(ref status, ErrorCode.PostconditionVerificationFailed, new[] { rewriteStep.Name, messageSource });
                }
            }
            catch (Exception ex)
            {
                this.LogAndUpdate(ref status, ex);
                var isLoadException = ex is FileLoadException || ex.InnerException is FileLoadException;
                if (isLoadException)
                {
                    this.LogAndUpdate(ref status, ErrorCode.FileNotFoundDuringPluginExecution, new[] { rewriteStep.Name, messageSource });
                }
                else
                {
                    this.LogAndUpdate(ref status, ErrorCode.PluginExecutionFailed, new[] { rewriteStep.Name, messageSource });
                }
            }

            if (status == Status.Succeeded)
            {
                this.CompilationOutput = newCompilation;
            }

            return status;
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
            this.compilationStatus.SourceFileLoading = 0;
            if (sources == null)
            {
                this.LogAndUpdate(ref this.compilationStatus.SourceFileLoading, ErrorCode.SourceFilesMissing);
            }

            void OnException(Exception ex) => this.LogAndUpdate(ref this.compilationStatus.SourceFileLoading, ex);
            void OnDiagnostic(Diagnostic d) => this.LogAndUpdateLoadDiagnostics(ref this.compilationStatus.SourceFileLoading, d);
            var sourceFiles = ProjectManager.LoadSourceFiles(sources ?? Enumerable.Empty<string>(), OnDiagnostic, OnException);
            this.PrintResolvedFiles(sourceFiles.Keys);
            return sourceFiles;
        }

        /// <summary>
        /// Used to load the content of the specified assembly references from disk.
        /// If loadTestNames is set to true, then public types and callables declared in referenced assemblies
        /// are exposed via their test name defined by the corresponding attribute.
        /// Returns the loaded content of the references.
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly.
        /// Prints all loaded files using PrintResolvedAssemblies.
        /// </summary>
        private References LoadAssemblies(IEnumerable<string> refs, bool loadTestNames, bool ignoreDllResources)
        {
            this.compilationStatus.ReferenceLoading = 0;
            if (refs == null)
            {
                this.LogAndUpdate(ref this.compilationStatus.ReferenceLoading, WarningCode.ReferencesSetToNull);
            }

            void OnException(Exception ex) => this.LogAndUpdate(ref this.compilationStatus.ReferenceLoading, ex);
            void OnDiagnostic(Diagnostic d) => this.LogAndUpdateLoadDiagnostics(ref this.compilationStatus.ReferenceLoading, d);

            // Skip loading any assemblies referenced as target packages. These will be included in a later
            // override step.
            var filteredRefs = refs ?? Enumerable.Empty<string>();
            if (this.config.TargetPackageAssemblies is object)
            {
                var targetPackagePaths = this.config.TargetPackageAssemblies.Select(a => Path.GetFullPath(a));
                filteredRefs = filteredRefs.Except(targetPackagePaths);
            }

            var headers = ProjectManager.LoadReferencedAssembliesInParallel(
                filteredRefs,
                OnDiagnostic,
                OnException,
                ignoreDllResources);

            var projId = this.config.ProjectName == null ? null : Path.ChangeExtension(Path.GetFullPath(this.config.ProjectNameWithExtension), "qsproj");
            var references = new References(headers, loadTestNames, (code, args) => OnDiagnostic(Errors.LoadError(code, args, projId)));
            this.PrintResolvedAssemblies(references.Declarations.Keys);
            return references;
        }

        /// <summary>
        /// Writes a binary representation of the built Q# compilation output to the given memory stream.
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly.
        /// Does *not* close the given memory stream, and
        /// returns true if the serialization has been successfully generated.
        /// </summary>
        private bool WriteSyntaxTreeSerialization(MemoryStream ms)
        {
            void LogError() => this.LogAndUpdate(
                ref this.compilationStatus.Serialization, ErrorCode.SerializationFailed);

            void LogExceptionAndError(Exception ex)
            {
                this.LogAndUpdate(ref this.compilationStatus.Serialization, ex);

                LogError();
            }

            this.compilationStatus.Serialization = Status.Succeeded;
            if (this.CompilationOutput == null)
            {
                LogError();
                return false;
            }

            var fromSources = this.CompilationOutput.Namespaces.Select(ns => FilterBySourceFile.Apply(ns, s => s.EndsWith(".qs")));
            var compilation = new QsCompilation(fromSources.ToImmutableArray(), this.CompilationOutput.EntryPoints);
            return SerializeSyntaxTree(compilation, ms, LogExceptionAndError);
        }

        /// <summary>
        /// Backtracks to the beginning of the given memory stream and writes its content to disk,
        /// generating a suitable bson file in the specified build output folder using the project name as file name.
        /// Generates a file name at random if no project name is specified.
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly.
        /// Returns the absolute path of the file where the binary representation has been generated.
        /// Returns null if the binary file could not be generated.
        /// Does *not* close the given memory stream.
        /// </summary>
        private string? GenerateBinary(MemoryStream serialization)
        {
            this.compilationStatus.BinaryFormat = Status.Succeeded;

            var projId = Path.GetFullPath(this.config.ProjectNameWithExtension ?? Path.GetRandomFileName());
            var outFolder = Path.GetFullPath(string.IsNullOrWhiteSpace(this.config.BuildOutputFolder) ? "." : this.config.BuildOutputFolder);
            var target = GeneratedFile(projId, outFolder, ".bson", "");

            try
            {
                serialization.Seek(0, SeekOrigin.Begin);
                using (var file = new FileStream(target, FileMode.Create, FileAccess.Write))
                {
                    serialization.WriteTo(file);
                }

                return target;
            }
            catch (Exception ex)
            {
                this.LogAndUpdate(ref this.compilationStatus.BinaryFormat, ex);
                this.LogAndUpdate(ref this.compilationStatus.BinaryFormat, ErrorCode.GeneratingBinaryFailed);
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
        /// </summary>
        private string? GenerateDll(MemoryStream serialization)
        {
            this.compilationStatus.DllGeneration = 0;

            var fallbackFileName = (this.PathToCompiledBinary ?? this.config.ProjectNameWithExtension) ?? Path.GetRandomFileName();
            var outputPath = Path.GetFullPath(string.IsNullOrWhiteSpace(this.config.DllOutputPath) ? fallbackFileName : this.config.DllOutputPath);
            outputPath = Path.ChangeExtension(outputPath, "dll");

            MetadataReference CreateReference(string file, int id) =>
                MetadataReference.CreateFromFile(file)
                .WithAliases(new string[] { $"{DotnetCoreDll.ReferenceAlias}{id}" }); // referenced Q# dlls are recognized based on this alias

            // We need to force the inclusion of references despite that we do not include C# code that depends on them.
            // This is done via generating a certain handle in all dlls built via this compilation loader.
            // This checks if that handle is available to merely generate a warning if we can't include the reference.
            bool CanBeIncluded(string dll)
            {
                // no need to throw in case this fails - ignore the reference instead
                try
                {
                    using var stream = File.OpenRead(dll);
                    using var assemblyFile = new PEReader(stream);
                    var metadataReader = assemblyFile.GetMetadataReader();
                    return metadataReader.TypeDefinitions
                        .Select(metadataReader.GetTypeDefinition)
                        .Any(t => metadataReader.GetString(t.Namespace) == DotnetCoreDll.MetadataNamespace);
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                var referencePaths = this.CompilationOutput?.Namespaces
                    .Apply(ns => GetSourceFiles.Apply(ns)) // we choose to keep only Q# references that have been used
                    .Where(file => file.EndsWith(".dll"));
                var references = referencePaths.Select((dll, id) => (dll, CreateReference(dll, id), CanBeIncluded(dll))).ToImmutableArray();
                var csharpTree = MetadataGeneration.GenerateAssemblyMetadata(references.Where(r => r.Item3).Select(r => r.Item2));
                foreach (var (dropped, _, _) in references.Where(r => !r.Item3))
                {
                    this.LogAndUpdate(ref this.compilationStatus.DllGeneration, WarningCode.ReferencesSetToNull, dropped);
                }

                var compilation = CodeAnalysis.CSharp.CSharpCompilation.Create(
                    this.config.ProjectNameWithoutPathOrExtension ?? Path.GetFileNameWithoutExtension(outputPath),
                    syntaxTrees: new[] { csharpTree },
                    references: references.Select(r => r.Item2).Append(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)), // if System.Object can't be found a warning is generated
                    options: new CodeAnalysis.CSharp.CSharpCompilationOptions(outputKind: CodeAnalysis.OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Release));

                using var outputStream = File.OpenWrite(outputPath);
                serialization.Seek(0, SeekOrigin.Begin);
                var astResource = new CodeAnalysis.ResourceDescription(DotnetCoreDll.SyntaxTreeResourceName, () => serialization, true);
                var result = compilation.Emit(
                    outputStream,
                    options: new CodeAnalysis.Emit.EmitOptions(),
                    manifestResources: new CodeAnalysis.ResourceDescription[] { astResource });

                var errs = result.Diagnostics.Where(d => d.Severity >= CodeAnalysis.DiagnosticSeverity.Error);
                if (errs.Any())
                {
                    throw new Exception($"error(s) on emitting dll: {Environment.NewLine}{string.Join(Environment.NewLine, errs.Select(d => d.GetMessage()))}");
                }

                return outputPath;
            }
            catch (Exception ex)
            {
                this.LogAndUpdate(ref this.compilationStatus.DllGeneration, ex);
                this.LogAndUpdate(ref this.compilationStatus.DllGeneration, ErrorCode.GeneratingDllFailed);
                return null;
            }
        }

        /// <summary>
        /// Given the path to a Q# binary file, reads the content of that file and returns the corresponding compilation as out parameter.
        /// Throws the corresponding exception if the given path does not correspond to a suitable binary file.
        /// </summary>
        public static bool ReadBinary(string file, [NotNullWhen(true)] out QsCompilation? syntaxTree) =>
            AssemblyLoader.LoadSyntaxTree(File.ReadAllBytes(Path.GetFullPath(file)), out syntaxTree);

        /// <summary>
        /// Given a stream with the content of a Q# binary file, returns the corresponding compilation as out parameter.
        /// </summary>
        public static bool ReadBinary(Stream stream, [NotNullWhen(true)] out QsCompilation? syntaxTree)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return AssemblyLoader.LoadSyntaxTree(memoryStream.ToArray(), out syntaxTree);
        }

        private static bool SerializeSyntaxTree(QsCompilation syntaxTree, Stream stream, Action<Exception>? onException = null)
        {
            try
            {
                BondSchemas.Protocols.SerializeQsCompilationToSimpleBinary(syntaxTree, stream);
            }
            catch (Exception ex)
            {
                onException?.Invoke(ex);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Writes a binary representation of the Q# compilation to supplied stream.
        /// </summary>
        public static bool WriteBinary(QsCompilation syntaxTree, Stream stream) =>
            SerializeSyntaxTree(syntaxTree, stream);

        /// <summary>
        /// Given a file id assigned by the Q# compiler, computes the corresponding path in the specified output folder.
        /// Returns the computed absolute path for a file with the specified ending.
        /// If the content for that file is specified, writes that content to disk.
        /// Throws the corresponding exception if any of the path operations fails or if the writing fails.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="fileId"/> is incompatible with an id assigned by the Q# compiler.</exception>
        /// <exception>Generates the corresponding exception if the file cannot be created.</exception>
        public static string GeneratedFile(string fileId, string outputFolder, string fileEnding, string? content = null)
        {
            if (!CompilationUnitManager.TryGetUri(fileId, out var file))
            {
                throw new ArgumentException("the given file id is not consistent with an id generated by the Q# compiler");
            }

            string FullDirectoryName(string dir) =>
                Path.GetFullPath(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);

            fileEnding = fileEnding.Trim().TrimStart('.');
            outputFolder = string.IsNullOrWhiteSpace(outputFolder) ? "." : outputFolder;
            var outputUri = new Uri(FullDirectoryName(outputFolder));
            var currentDir = new Uri(FullDirectoryName("."));
            var relFilePath = currentDir.MakeRelativeUri(file);
            var filePath = Uri.UnescapeDataString(new Uri(outputUri, relFilePath).LocalPath);
            var fileDir = filePath.StartsWith(outputUri.LocalPath)
                ? Path.GetDirectoryName(filePath)
                : Path.GetDirectoryName(outputUri.LocalPath);
            var targetFile = Path.GetFullPath(Path.Combine(fileDir, $"{Path.GetFileNameWithoutExtension(filePath)}.{fileEnding}"));

            if (content == null)
            {
                return targetFile;
            }

            if (!Directory.Exists(fileDir))
            {
                Directory.CreateDirectory(fileDir);
            }

            File.WriteAllText(targetFile, content);
            return targetFile;
        }
    }
}
