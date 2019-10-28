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
        /// Given a load function that loads the content of a sequence of refernced assemblies from disk,
        /// returns the loaded references for the compilation. 
        /// </summary>
        public delegate References ReferenceLoader(Func<IEnumerable<string>, References> loadFromDisk);
        /// <summary>
        /// Concrete implementation of the rewrite steps options such that the configured options may be null. 
        /// </summary>
        private class RewriteStepOptions : IRewriteStepOptions
        {
            public readonly string OutputFolder;
            internal RewriteStepOptions(Configuration config) =>
                this.OutputFolder = config.BuildOutputFolder;
        }


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
            /// Contains a sequence of tuples with the path to a dotnet dll containing one or more rewrite steps 
            /// (i.e. classes implementing IRewriteStep) and a function to get the corresponding rewrite step options.
            /// The contained rewrite steps will be executed in the defined order and priority at the end of the compilation. 
            /// </summary>
            public IEnumerable<(string, IRewriteStepOptions)> RewriteSteps;

            /// <summary>
            /// Indicates whether a serialization of the syntax tree needs to be generated. 
            /// This is the case if either the build output folder is specified or the dll output path is specified.
            /// </summary>
            internal bool SerializeSyntaxTree =>
                BuildOutputFolder != null || DllOutputPath != null;

            /// <summary>
            /// Returns the default options used for rewrite steps if no options are specified, i.e. the given options are null.
            /// </summary>
            public IRewriteStepOptions RewriteStepDefaultOptions => 
                new RewriteStepOptions(this);
        }

        private class ExecutionStatus
        {
            internal int SourceFileLoading = -1;
            internal int ReferenceLoading = -1;
            internal int Validation = -1;
            internal int FunctorSupport = -1;
            internal int PreEvaluation = -1;
            internal int TreeTrimming = -1;
            internal int Documentation = -1;
            internal int Serialization = -1;
            internal int BinaryFormat = -1;
            internal int DllGeneration = -1;
            internal Dictionary<string, int> BuildTargets;

            internal ExecutionStatus(IEnumerable<string> targets) =>
                this.BuildTargets = targets.ToDictionary(id => id, _ => -1);

            private bool WasSuccessful(bool run, int code) =>
                (run && code == 0) || (!run && code < 0);

            internal bool Success(Configuration options) =>
                this.SourceFileLoading <= 0 &&
                this.ReferenceLoading <= 0 &&
                WasSuccessful(true, this.Validation) &&
                WasSuccessful(options.GenerateFunctorSupport, this.FunctorSupport) &&
                WasSuccessful(options.AttemptFullPreEvaluation, this.PreEvaluation) &&
                WasSuccessful(!options.SkipSyntaxTreeTrimming, this.TreeTrimming) &&
                WasSuccessful(options.DocumentationOutputFolder != null, this.Documentation) &&
                WasSuccessful(options.SerializeSyntaxTree, this.Serialization) &&
                WasSuccessful(options.BuildOutputFolder != null, this.BinaryFormat) &&
                WasSuccessful(options.DllOutputPath != null, this.DllGeneration) &&
                !this.BuildTargets.Values.Any(status => !WasSuccessful(true, status))
                ? true : false;
        }

        /// <summary>
        /// used to indicate the status of individual compilation steps
        /// </summary>
        public enum Status { NotRun, Succeeded, Failed }
        private Status GetStatus(int value) =>
            value < 0 ? Status.NotRun :
            value == 0 ? Status.Succeeded :
            Status.Failed;

        /// <summary>
        /// Indicates whether all source files were loaded successfully.
        /// Source file loading may not be executed if the content was preloaded using methods outside this class. 
        /// </summary>
        public Status SourceFileLoading => GetStatus(this.CompilationStatus.SourceFileLoading);
        /// <summary>
        /// Indicates whether all references were loaded successfully.
        /// The loading may not be executed if all references were preloaded using methods outside this class. 
        /// </summary>
        public Status ReferenceLoading => GetStatus(this.CompilationStatus.ReferenceLoading);
        /// <summary>
        /// Indicates whether the compilation unit passed the compiler validation 
        /// that is executed before invoking further rewrite and/or generation steps.   
        /// </summary>
        public Status Validation => GetStatus(this.CompilationStatus.Validation);
        /// <summary>
        /// Indicates whether all specializations were generated successfully. 
        /// This rewrite step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status FunctorSupport => GetStatus(this.CompilationStatus.FunctorSupport);
        /// <summary>
        /// Indicates whether the pre-evaluation step executed successfully. 
        /// This rewrite step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status PreEvaluation => GetStatus(this.CompilationStatus.PreEvaluation);
        /// <summary>
        /// Indicates whether documentation for the compilation was generated successfully. 
        /// This step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status Documentation => GetStatus(this.CompilationStatus.Documentation);
        /// <summary>
        /// Indicates whether the built compilation could be serialized successfully. 
        /// This step is only executed if either the binary representation or a dll is emitted. 
        /// </summary>
        public Status Serialization => GetStatus(this.CompilationStatus.Serialization);
        /// <summary>
        /// Indicates whether a binary representation for the generated syntax tree has been generated successfully. 
        /// This step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status BinaryFormat => GetStatus(this.CompilationStatus.BinaryFormat);
        /// <summary>
        /// Indicates whether a dll containing the compiled binary has been generated successfully. 
        /// This step is only executed if the corresponding configuration is specified. 
        /// </summary>
        public Status DllGeneration => GetStatus(this.CompilationStatus.DllGeneration);
        /// <summary>
        /// Indicates whether the specified build target executed successfully. 
        /// Returns a status NotRun if no target with the given id was listed for execution in the set configuration. 
        /// Execution is considered successful if the targets invokation did not throw an exception and returned true. 
        /// </summary>
        public Status Target(string id) => this.CompilationStatus.BuildTargets.TryGetValue(id, out var status) ? GetStatus(status) : Status.NotRun;
        /// <summary>
        /// Indicates the overall status of all specified build targets.
        /// The status is indicated as success if none of the specified build targets failed. 
        /// </summary>
        public Status AllTargets => this.CompilationStatus.BuildTargets.Values.Any(s => GetStatus(s) == Status.Failed) ? Status.Failed : Status.Succeeded;
        /// <summary>
        /// Indicates the overall success of all compilation steps. 
        /// The compilation is indicated as having been successful if all steps that were configured to execute completed successfully.
        /// </summary>
        public bool Success => this.CompilationStatus.Success(this.Config);


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
        public IEnumerable<QsNamespace> GeneratedSyntaxTree => this.CompilationOutput?.Namespaces;

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
            this.Config = options ?? new Configuration();
            this.CompilationStatus = new ExecutionStatus(this.Config.RewriteSteps?.Select(step => step.Item1) ?? Enumerable.Empty<string>());
            this.LoadDiagnostics = ImmutableArray<Diagnostic>.Empty;
            var sourceFiles = loadSources?.Invoke(this.LoadSourceFiles) ?? throw new ArgumentNullException("unable to load source files");
            var references = loadReferences?.Invoke(this.LoadAssemblies) ?? throw new ArgumentNullException("unable to load referenced binary files");

            // building the compilation

            this.CompilationStatus.Validation = 0;
            var files = CompilationUnitManager.InitializeFileManagers(sourceFiles, null, this.OnCompilerException); // do *not* live track (i.e. use publishing) here!
            var compilationManager = new CompilationUnitManager(this.OnCompilerException);
            compilationManager.UpdateReferencesAsync(references);
            compilationManager.AddOrUpdateSourceFilesAsync(files);
            this.VerifiedCompilation = compilationManager.Build();
            this.CompilationOutput = this.VerifiedCompilation?.BuiltCompilation;

            foreach (var diag in this.VerifiedCompilation.SourceFiles?.SelectMany(this.VerifiedCompilation.Diagnostics) ?? Enumerable.Empty<Diagnostic>())
            { this.LogAndUpdate(ref this.CompilationStatus.Validation, diag); }

            // executing the specified rewrite steps 

            if (this.Config.GenerateFunctorSupport)
            {
                this.CompilationStatus.FunctorSupport = 0;
                void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.FunctorSupport, ex);
                var generated = this.CompilationOutput != null && CodeGeneration.GenerateFunctorSpecializations(this.CompilationOutput, out this.CompilationOutput, onException);
                if (!generated) this.LogAndUpdate(ref this.CompilationStatus.FunctorSupport, ErrorCode.FunctorGenerationFailed, Enumerable.Empty<string>());
            }

            if (!this.Config.SkipSyntaxTreeTrimming)
            {
                this.CompilationStatus.TreeTrimming = 0;
                void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.TreeTrimming, ex);
                var trimmed = this.CompilationOutput != null && CodeTransformations.InlineConjugations(this.CompilationOutput, out this.CompilationOutput, onException);
                if (!trimmed) this.LogAndUpdate(ref this.CompilationStatus.TreeTrimming, ErrorCode.TreeTrimmingFailed, Enumerable.Empty<string>());
            }

            if (this.Config.AttemptFullPreEvaluation)
            {
                this.CompilationStatus.PreEvaluation = 0;
                void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.PreEvaluation, ex);
                var evaluated = this.CompilationOutput != null && CodeTransformations.PreEvaluateAll(this.CompilationOutput, out this.CompilationOutput, onException);
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
                this.CompilationStatus.Documentation = 0;
                var docsFolder = Path.GetFullPath(String.IsNullOrWhiteSpace(this.Config.DocumentationOutputFolder) ? "." : this.Config.DocumentationOutputFolder);
                void onDocException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.Documentation, ex);
                var docsGenerated = this.VerifiedCompilation != null && DocBuilder.Run(docsFolder, this.VerifiedCompilation.SyntaxTree.Values, this.VerifiedCompilation.SourceFiles, onException: onDocException);
                if (!docsGenerated) this.LogAndUpdate(ref this.CompilationStatus.Documentation, ErrorCode.DocGenerationFailed, Enumerable.Empty<string>());
            }

            // invoking the given targets



            foreach (var (target, rewriteStepOptions) in this.Config.RewriteSteps ?? Enumerable.Empty<(string, IRewriteStepOptions)>())
            {
                this.CompilationStatus.BuildTargets[target] = 0;
                // try ...
                var assembly = Assembly.LoadFrom(target); // fixme: what if it doesn't exist etc
                var rewriteSteps = assembly.GetTypes().Where(t => t.IsAssignableFrom(typeof(IRewriteStep))).Select(step =>
                {
                    try { return Activator.CreateInstance(step) as IRewriteStep; }
                    catch { return null; } // FIXME: fail if no parameterless constructor is available
                })
                .Where(step => step != null).ToList(); // fixme: messages for null
                rewriteSteps.Sort((fst, snd) => snd.Priority - fst.Priority); // fixme: check ordering

                foreach (var rewriteStep in rewriteSteps)
                { 
                    rewriteStep.Options = rewriteStepOptions ?? rewriteStep.Options ?? this.Config.RewriteStepDefaultOptions;
                    var executeTransformation = (!rewriteStep.ImplementsPreconditionVerification || rewriteStep.PreconditionVerification(this.CompilationOutput)) && rewriteStep.ImplementsTransformation; // FIXME: error handling
                    var executed = executeTransformation && rewriteStep.Transformation(this.CompilationOutput, out this.CompilationOutput); // FIME
                    var succeeded = executed && (!rewriteStep.ImplementsPostconditionVerification || rewriteStep.PostconditionVerification(this.CompilationOutput)); // FIXME
                }

                //var succeeded = this.PathToCompiledBinary != null && buildTarget.Value != null &&
                //    buildTarget.Value(this.PathToCompiledBinary, ex => this.LogAndUpdate(buildTarget.Key, ex));
                //if (!succeeded) this.LogAndUpdate(target, ErrorCode.TargetExecutionFailed, new[] { buildTarget.Key });
            } 
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
        private void LogAndUpdate(ref int current, Diagnostic d)
        {
            this.Logger?.Log(d);
            if (d.IsError()) current = 1;
        }

        /// <summary>
        /// Logs the given exception and updates the status passed as reference accordingly. 
        /// </summary>
        private void LogAndUpdate(ref int current, Exception ex)
        {
            this.Logger?.Log(ex);
            current = 1;
        }

        /// <summary>
        /// Logs an error with the given error code and message parameters, and updates the status passed as reference accordingly. 
        /// </summary>
        private void LogAndUpdate(ref int current, ErrorCode code, IEnumerable<string> args)
        {
            this.Logger?.Log(code, args);
            current = 1;
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
        /// Logs the given exception and updates the status of the specified target accordingly. 
        /// Throws an ArgumentException if no build target with the given id exists. 
        /// </summary>
        private void LogAndUpdate(string targetId, Exception ex)
        {
            if (!this.CompilationStatus.BuildTargets.TryGetValue(targetId, out var current)) throw new ArgumentException("unknown target");
            this.LogAndUpdate(ref current, ex);
            this.CompilationStatus.BuildTargets[targetId] = current;
        }

        /// <summary>
        /// Logs an error with the given error code and message parameters, and updates the status of the specified target accordingly. 
        /// Throws an ArgumentException if no build target with the given id exists. 
        /// </summary>
        private void LogAndUpdate(string targetId, ErrorCode code, IEnumerable<string> args)
        {
            if (!this.CompilationStatus.BuildTargets.TryGetValue(targetId, out var current)) throw new ArgumentException("unknown target");
            this.LogAndUpdate(ref current, code, args);
            this.CompilationStatus.BuildTargets[targetId] = current;
        }

        /// <summary>
        /// Logs the names of the given source files as Information unless the given argument is null.
        /// </summary>
        private void PrintResolvedFiles(IEnumerable<Uri> sourceFiles)
        {
            if (sourceFiles == null) return;
            var args = sourceFiles.Any()
                ? sourceFiles.Select(f => f?.LocalPath).ToArray()
                : new string[] { "(none)" };
            this.Logger?.Log(InformationCode.CompilingWithSourceFiles, Enumerable.Empty<string>(), messageParam: Diagnostics.Formatting.Indent(args).ToArray());
        }

        /// <summary>
        /// Logs the names of the given assemblies as Information unless the given argument is null.
        /// </summary>
        private void PrintResolvedAssemblies(IEnumerable<NonNullable<string>> assemblies)
        {
            if (assemblies == null) return;
            var args = assemblies.Any()
                ? assemblies.Select(name => name.Value).ToArray()
                : new string[] { "(none)" };
            this.Logger?.Log(InformationCode.CompilingWithAssemblies, Enumerable.Empty<string>(), messageParam: Diagnostics.Formatting.Indent(args).ToArray());
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
            void onDiagnostic(Diagnostic d)
            {
                this.LoadDiagnostics = this.LoadDiagnostics.Add(d);
                this.LogAndUpdate(ref this.CompilationStatus.SourceFileLoading, d);
            }
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
        private References LoadAssemblies(IEnumerable<string> refs) 
        {
            this.CompilationStatus.ReferenceLoading = 0;
            if (refs == null) this.Logger?.Log(WarningCode.ReferencesSetToNull, Enumerable.Empty<string>());
            void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.ReferenceLoading, ex);
            void onDiagnostic(Diagnostic d)
            {
                this.LoadDiagnostics = this.LoadDiagnostics.Add(d);
                this.LogAndUpdate(ref this.CompilationStatus.ReferenceLoading, d);
            }
            var headers = ProjectManager.LoadReferencedAssemblies(refs ?? Enumerable.Empty<string>(), onDiagnostic, onException);
            var projId =this.Config.ProjectName == null ? null : Path.ChangeExtension(Path.GetFullPath(this.Config.ProjectName), "qsproj");
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
            bool ErrorAndReturn ()
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

            var projId = NonNullable<string>.New(Path.GetFullPath(this.Config.ProjectName ?? Path.GetRandomFileName()));
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

            var fallbackFileName = (this.PathToCompiledBinary ?? this.Config.ProjectName) ?? Path.GetRandomFileName();
            var outputPath = Path.GetFullPath(String.IsNullOrWhiteSpace(this.Config.DllOutputPath) ? fallbackFileName : this.Config.DllOutputPath);
            outputPath = Path.ChangeExtension(outputPath, "dll");

            MetadataReference CreateReference(string file, int id) =>
                MetadataReference.CreateFromFile(file)
                .WithAliases(new string[] { $"{AssemblyConstants.QSHARP_REFERENCE}{id}" }); // referenced Q# dlls are recognized based on this alias 

            // We need to force the inclusion of references despite that that we do not include C# code that depends on them. 
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
                        .Any(t => metadataReader.GetString(t.Namespace) == AssemblyConstants.METADATA_NAMESPACE);
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
                    this.Config.ProjectName ?? Path.GetFileNameWithoutExtension(outputPath),
                    syntaxTrees: new[] { csharpTree },
                    references: references.Select(r => r.Item2).Append(MetadataReference.CreateFromFile(typeof(object).Assembly.Location)), // if System.Object can't be found a warning is generated
                    options: new CodeAnalysis.CSharp.CSharpCompilationOptions(outputKind: CodeAnalysis.OutputKind.DynamicallyLinkedLibrary)
                );
                
                using var outputStream = File.OpenWrite(outputPath);
                serialization.Seek(0, SeekOrigin.Begin);
                var astResource = new CodeAnalysis.ResourceDescription(AssemblyConstants.AST_RESOURCE_NAME, () => serialization, true);
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
        /// Given the path to a Q# binary file, reads the content of that file and returns the corresponding syntax tree as out parameter. 
        /// Throws the corresponding exception if the given path does not correspond to a suitable binary file.
        /// May throw an exception if the given binary file has been compiled with a different compiler version.
        /// </summary>
        public static void ReadBinary(string file, out IEnumerable<QsNamespace> syntaxTree) =>
            ReadBinary(new MemoryStream(File.ReadAllBytes(Path.GetFullPath(file))), out syntaxTree);

        /// <summary>
        /// Given a stream with the content of a Q# binary file, returns the corresponding syntax tree as out parameter.
        /// Throws an ArgumentNullException if the given stream is null.
        /// May throw an exception if the given binary file has been compiled with a different compiler version.
        /// </summary>
        public static void ReadBinary(Stream stream, out IEnumerable<QsNamespace> syntaxTree)
        { syntaxTree = AssemblyLoader.LoadSyntaxTree(stream).Namespaces; }

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
