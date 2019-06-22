// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.Documentation;
using Microsoft.Quantum.QsCompiler.Serialization;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;


namespace Microsoft.Quantum.QsCompiler
{
    public class CompilationLoader
    {
        /// Given a load function that loads the content of a sequence of files from disk, returns the content for all sources to compile. 
        public delegate ImmutableDictionary<Uri, string> SourceLoader(Func<IEnumerable<string>, ImmutableDictionary<Uri, string>> loadFromDisk);
        /// Given a load function that loads the content of a sequence of refernced assemblies from disk, returns the loaded references for the compilation. 
        public delegate References ReferenceLoader(Func<IEnumerable<string>, References> loadFromDisk);
        /// Processes a compiled Q# binary file given its path, returns true or false to indicate its success or failure, and calls the given action on any thrown exception. 
        public delegate bool BuildTarget(string pathToBinary, Action<Exception> onException); 


        /// may be specified via configuration (or project) file in the future
        public struct Configuration
        {
            /// Uri to the project file (if any). 
            /// The name of the project file with a suitable extension will be used as the name of the generated binary file.
            public Uri ProjectFile;
            /// If set to true, the syntax tree rewrite step that replaces all generation directives 
            /// for all functor specializations is executed during compilation.   
            public bool GenerateFunctorSupport;
            /// If the output folder is not null, 
            /// documentation is generated in the specified folder based on doc comments in the source code. 
            public string DocumentationOutputFolder;
            /// Directory where the compiled binaries will be generated. 
            /// No binaries will be written to disk unless this path is specified and valid. 
            public string BuildOutputFolder;
            /// Dictionary that maps an arbitarily chosen target name to the build targets to call with the path to the compiled binary.
            /// The specified targets (dictionary values) will only be invoked if a binary file was generated successfully.
            public ImmutableDictionary<string, BuildTarget> Targets;
        }

        private class ExecutionStatus
        {
            internal int SourceFileLoading = -1;
            internal int ReferenceLoading = -1;
            internal int Validation = -1;
            internal int FunctorSupport = -1;
            internal int Documentation = -1;
            internal int BinaryFormat = -1;
            internal Dictionary<string, int> BuildTargets;

            internal ExecutionStatus(IEnumerable<string> targets) =>
                this.BuildTargets = targets.ToDictionary(id => id, _ => -1);

            private bool WasSuccessful(bool run, int code) =>
                (run && code == 0) || (!run && code < 0);

            internal int Success(Configuration options) =>
                this.SourceFileLoading <= 0 &&
                this.ReferenceLoading <= 0 &&
                WasSuccessful(true, this.Validation) &&
                WasSuccessful(options.GenerateFunctorSupport, this.FunctorSupport) &&
                WasSuccessful(options.DocumentationOutputFolder != null, this.Documentation) &&
                WasSuccessful(options.BuildOutputFolder != null, this.BinaryFormat) &&
                !this.BuildTargets.Values.Any(status => !WasSuccessful(true, status))
                ? 0 : 1;
        }

        /// used to indicate the status of individual compilation steps
        public enum Status { NotRun, Succeeded, Failed }
        private Status GetStatus(int value) =>
            value < 0 ? Status.NotRun :
            value == 0 ? Status.Succeeded :
            Status.Failed;

        /// Indicates whether all source files were loaded successfully.
        /// Source file loading may not be executed if the content was preloaded using methods outside this class. 
        public Status SourceFileLoading => GetStatus(this.CompilationStatus.SourceFileLoading);
        /// Indicates whether all references were loaded successfully.
        /// The loading may not be executed if all references were preloaded using methods outside this class. 
        public Status ReferenceLoading => GetStatus(this.CompilationStatus.ReferenceLoading);
        /// Indicates whether the compilation unit passed the compiler validation 
        /// that is executed before invoking further rewrite and/or generation steps.   
        public Status Validation => GetStatus(this.CompilationStatus.Validation);
        /// Indicates whether all specializations were generated successfully. 
        /// This rewrite step is only executed if the corresponding configuration is specified. 
        public Status FunctorSupport => GetStatus(this.CompilationStatus.FunctorSupport);
        /// Indicates whether documentation for the compilation was generated successfully. 
        /// This step is only executed if the corresponding configuration is specified. 
        public Status Documentation => GetStatus(this.CompilationStatus.Documentation);
        /// Indicates whether a binary representation for the generated syntax tree has been generated successfully. 
        /// This step is only executed if the corresponding configuration is specified. 
        public Status BinaryFormat => GetStatus(this.CompilationStatus.BinaryFormat);
        /// Indicates whether the specified build target executed successfully. 
        /// Returns a status NotRun if no target with the given id was listed for execution in the set configuration. 
        /// Execution is considered successful if the targets invokation did not throw an exception and returned true. 
        public Status Target(string id) => this.CompilationStatus.BuildTargets.TryGetValue(id, out var status) ? GetStatus(status) : Status.NotRun;
        /// Indicates the overall status of all specified build targets.
        /// The status is indicated as success if none of the specified build targets failed. 
        public Status AllTargets => this.CompilationStatus.BuildTargets.Values.Any(s => GetStatus(s) == Status.Failed) ? Status.Failed : Status.Succeeded;
        /// Indicates the overall success of all compilation steps. 
        /// The compilation is indicated as having been successful if all steps that were configured to execute completed successfully.
        public Status Success => GetStatus(this.CompilationStatus.Success(this.Config));


        /// logger used to log all diagnostic events during compilation
        private readonly ILogger Logger;
        /// configuration specifying the compilation steps to execute
        private readonly Configuration Config;
        /// used to track the status of individual compilation steps
        private ExecutionStatus CompilationStatus;
        /// contains the initial compilation built by the compilation unit manager after verification
        public readonly CompilationUnitManager.Compilation VerifiedCompilation;
        /// contains the syntax tree after executing all configured rewrite steps
        public readonly IEnumerable<QsNamespace> GeneratedSyntaxTree;
        /// contains the absolute path where the binary representation of the generated syntax tree has been written to disk
        public readonly string PathToCompiledBinary;

        /// Builds the compilation for the source files and references loaded by the given loaders,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events. 
        /// Throws an ArgumentNullException if either one of the given loaders is null or returns null.
        public CompilationLoader(SourceLoader loadSources, ReferenceLoader loadReferences, Configuration? options = null, ILogger logger = null)
        {
            // loading the content to compiler 

            this.Logger = logger;
            this.Config = options ?? new Configuration();
            this.CompilationStatus = new ExecutionStatus(this.Config.Targets?.Keys ?? Enumerable.Empty<string>());
            var sourceFiles = loadSources?.Invoke(this.LoadSourceFiles) ?? throw new ArgumentNullException("unable to load source files");
            var references = loadReferences?.Invoke(this.LoadAssemblies) ?? throw new ArgumentNullException("unable to load referenced binary files");

            // building the compilation

            this.CompilationStatus.Validation = 0;
            var files = CompilationUnitManager.InitializeFileManagers(sourceFiles, null, this.OnCompilerException); // do *not* live track (i.e. use publishing) here!
            var compilationManager = new CompilationUnitManager(this.OnCompilerException);
            compilationManager.UpdateReferencesAsync(references);
            compilationManager.AddOrUpdateSourceFilesAsync(files);
            this.VerifiedCompilation = compilationManager.Build();
            this.GeneratedSyntaxTree = this.VerifiedCompilation?.SyntaxTree.Values;

            foreach (var diag in this.VerifiedCompilation.SourceFiles?.SelectMany(this.VerifiedCompilation.Diagnostics) ?? Enumerable.Empty<Diagnostic>())
            { this.LogAndUpdate(ref this.CompilationStatus.Validation, diag); }

            // executing the specified rewrite steps 

            if (this.Config.GenerateFunctorSupport)
            {
                this.CompilationStatus.FunctorSupport = 0;
                var functorSpecGenerated = this.VerifiedCompilation != null && FunctorGeneration.GenerateFunctorSpecializations(this.GeneratedSyntaxTree, out this.GeneratedSyntaxTree);
                if (!functorSpecGenerated) this.LogAndUpdate(ref this.CompilationStatus.FunctorSupport, ErrorCode.FunctorGenerationFailed, Enumerable.Empty<string>());
            }

            // generating the compiled binary

            using (var ms = new MemoryStream())
            { this.PathToCompiledBinary = this.GenerateBinary(ms); }

            // executing the specified generation steps 

            if (this.Config.DocumentationOutputFolder != null)
            {
                this.CompilationStatus.Documentation = 0;
                var docsFolder = Path.GetFullPath(String.IsNullOrWhiteSpace(this.Config.DocumentationOutputFolder) ? "." : this.Config.DocumentationOutputFolder);
                void onDocException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.Documentation, ex);
                var (tree, sources) = (this.VerifiedCompilation?.SyntaxTree?.Values, this.VerifiedCompilation?.SyntaxTree?.Keys);
                var docsGenerated = this.VerifiedCompilation != null && DocBuilder.Run(docsFolder, tree, sources, onException: onDocException);
                if (!docsGenerated) this.LogAndUpdate(ref this.CompilationStatus.Documentation, ErrorCode.DocGenerationFailed, Enumerable.Empty<string>());
            }

            // invoking the given targets

            foreach (var buildTarget in this.Config.Targets ?? ImmutableDictionary<string, BuildTarget>.Empty)
            {
                this.CompilationStatus.BuildTargets[buildTarget.Key] = 0;
                var succeeded = this.PathToCompiledBinary != null && buildTarget.Value != null &&
                    buildTarget.Value(this.PathToCompiledBinary, ex => this.LogAndUpdate(buildTarget.Key, ex));
                if (!succeeded) this.LogAndUpdate(buildTarget.Key, ErrorCode.TargetExecutionFailed, new[] { buildTarget.Key });
            } 
        }

        /// Builds the compilation of the specified source files and references,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events. 
        public CompilationLoader(IEnumerable<string> sources, IEnumerable<string> references, Configuration? options = null, ILogger logger = null)
            : this(load => load(sources), load => load(references), options, logger) { }

        /// Builds the compilation of the specified source files and the loaded references returned by the given loader,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events. 
        /// Throws an ArgumentNullException if the given loader is null or returns null.
        public CompilationLoader(IEnumerable<string> sources, ReferenceLoader loadReferences, Configuration? options = null, ILogger logger = null)
            : this(load => load(sources), loadReferences, options, logger) { }

        /// Builds the compilation of the content returned by the given loader and the specified references,
        /// executing the compilation steps specified by the given options.
        /// Uses the specified logger to log all diagnostic events. 
        /// Throws an ArgumentNullException if the given loader is null or returns null.
        public CompilationLoader(SourceLoader loadSources, IEnumerable<string> references, Configuration? options = null, ILogger logger = null)
            : this(loadSources, load => load(references), options, logger) { }


        // private routines used for logging and status updates

        /// Logs the given diagnostic and updates the status passed as reference accordingly. 
        /// Throws an ArgumentNullException if the given diagnostic is null. 
        private void LogAndUpdate(ref int current, Diagnostic d)
        {
            this.Logger?.Log(d);
            if (d.IsError()) current = 1;
        }

        /// Logs the given exception and updates the status passed as reference accordingly. 
        private void LogAndUpdate(ref int current, Exception ex)
        {
            this.Logger?.Log(ex);
            current = 1;
        }

        /// Logs an error with the given error code and message parameters, and updates the status passed as reference accordingly. 
        private void LogAndUpdate(ref int current, ErrorCode code, IEnumerable<string> args)
        {
            this.Logger?.Log(code, args);
            current = 1;
        }

        /// Logs an UnexpectedCompilerException error as well as the given exception, and updates the validation status accordingly. 
        private void OnCompilerException(Exception ex)
        {
            this.LogAndUpdate(ref this.CompilationStatus.Validation, ErrorCode.UnexpectedCompilerException, Enumerable.Empty<string>());
            this.LogAndUpdate(ref this.CompilationStatus.Validation, ex);
        }

        /// Logs the given exception and updates the status of the specified target accordingly. 
        /// Throws an ArgumentException if no build target with the given id exists. 
        private void LogAndUpdate(string targetId, Exception ex)
        {
            if (!this.CompilationStatus.BuildTargets.TryGetValue(targetId, out var current)) throw new ArgumentException("unknown target");
            this.LogAndUpdate(ref current, ex);
            this.CompilationStatus.BuildTargets[targetId] = current;
        }

        /// Logs an error with the given error code and message parameters, and updates the status of the specified target accordingly. 
        /// Throws an ArgumentException if no build target with the given id exists. 
        private void LogAndUpdate(string targetId, ErrorCode code, IEnumerable<string> args)
        {
            if (!this.CompilationStatus.BuildTargets.TryGetValue(targetId, out var current)) throw new ArgumentException("unknown target");
            this.LogAndUpdate(ref current, code, args);
            this.CompilationStatus.BuildTargets[targetId] = current;
        }

        /// Logs the names of the given source files as Information unless the given argument is null.
        private void PrintResolvedFiles(IEnumerable<Uri> sourceFiles)
        {
            if (sourceFiles == null) return;
            var args = sourceFiles.Any()
                ? sourceFiles.Select(f => f?.LocalPath).ToArray()
                : new string[] { "(none)" };
            this.Logger?.Log(InformationCode.CompilingWithSourceFiles, Enumerable.Empty<string>(), messageParam: Diagnostics.Formatting.Indent(args).ToArray());
        }

        /// Logs the names of the given assemblies as Information unless the given argument is null.
        private void PrintResolvedAssemblies(IEnumerable<NonNullable<string>> assemblies)
        {
            if (assemblies == null) return;
            var args = assemblies.Any()
                ? assemblies.Select(name => name.Value).ToArray()
                : new string[] { "(none)" };
            this.Logger?.Log(InformationCode.CompilingWithAssemblies, Enumerable.Empty<string>(), messageParam: Diagnostics.Formatting.Indent(args).ToArray());
        }


        // routines for loading from and dumping to files

        /// Used to load the content of the specified source files from disk. 
        /// Returns a dictionary mapping the file uri to its content. 
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly. 
        /// Prints all loaded files using PrintResolvedFiles.
        private ImmutableDictionary<Uri, string> LoadSourceFiles(IEnumerable<string> sources)
        {
            this.CompilationStatus.SourceFileLoading = 0;
            if (sources == null) this.LogAndUpdate(ref this.CompilationStatus.SourceFileLoading, ErrorCode.SourceFilesMissing, Enumerable.Empty<string>());
            void onDiagnostic(Diagnostic d) => this.LogAndUpdate(ref this.CompilationStatus.SourceFileLoading, d);
            void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.SourceFileLoading, ex);
            var sourceFiles = ProjectManager.LoadSourceFiles(sources ?? Enumerable.Empty<string>(), onDiagnostic, onException);
            this.PrintResolvedFiles(sourceFiles.Keys);
            return sourceFiles;
        }

        /// Used to load the content of the specified assembly references from disk. 
        /// Returns the loaded content of the references. 
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly. 
        /// Prints all loaded files using PrintResolvedAssemblies.
        private References LoadAssemblies(IEnumerable<string> refs) 
        {
            this.CompilationStatus.ReferenceLoading = 0;
            if (refs == null) this.Logger?.Log(WarningCode.ReferencesSetToNull, Enumerable.Empty<string>());
            void onDiagnostic(Diagnostic d) => this.LogAndUpdate(ref this.CompilationStatus.ReferenceLoading, d);
            void onException(Exception ex) => this.LogAndUpdate(ref this.CompilationStatus.ReferenceLoading, ex);
            var references = ProjectManager.LoadReferencedAssemblies(refs ?? Enumerable.Empty<string>(), onDiagnostic, onException);
            this.PrintResolvedAssemblies(references.Declarations.Keys);
            return references;
        }

        /// Creates a binary representation of the generated syntax tree using the given memory stream. 
        /// Generates a file name at random and writes the content of that stream into a file within the specified build output folder. 
        /// Logs suitable diagnostics in the process and modifies the compilation status accordingly.
        /// Returns the absolute path of the file where the binary representation has been generated. 
        /// Returns null without doing anything if no build output folder is specified in the set configuration. 
        /// Does *not* close the given memory stream. 
        private string GenerateBinary(MemoryStream ms)
        {
            if (this.Config.BuildOutputFolder == null) return null;
            this.CompilationStatus.BinaryFormat = 0;            
            using (var writer = new BsonDataWriter(ms) { CloseOutput = false })
            {
                var settings = new JsonSerializerSettings { Converters = JsonConverters.All(false), ContractResolver = new DictionaryAsArrayResolver() };
                var serializer = JsonSerializer.CreateDefault(settings);
                if (this.GeneratedSyntaxTree != null) serializer.Serialize(writer, this.GeneratedSyntaxTree);
                else this.LogAndUpdate(ref this.CompilationStatus.BinaryFormat, ErrorCode.GeneratingBinaryFailed, Enumerable.Empty<string>());
            }

            var projId = NonNullable<string>.New(this.Config.ProjectFile?.AbsolutePath ?? Path.GetFullPath(Path.GetRandomFileName()));
            var target = GeneratedFile(projId, this.Config.BuildOutputFolder, ".bson", "");
            using (var file = new FileStream(target, FileMode.Create, FileAccess.Write))
            { ms.WriteTo(file); }
            return target;
        }

        /// Given the path to a Q# binary file, reads the content of that file and returns the corresponding syntax tree. 
        /// Throws the corresponding exception if the given path does not correspond to a suitable binary file.
        /// Potentially throws an exception in particular also if the given binary file has been compiled with a different compiler version. 
        public static IEnumerable<QsNamespace> ReadBinary(string file)
        {
            byte[] binary = File.ReadAllBytes(Path.GetFullPath(file));
            var ms = new MemoryStream(binary);
            using (var reader = new BsonDataReader(ms))
            {
                reader.ReadRootValueAsArray = true;
                var settings = new JsonSerializerSettings { Converters = JsonConverters.All(false), ContractResolver = new DictionaryAsArrayResolver() };
                var serializer = JsonSerializer.CreateDefault(settings);
                return serializer.Deserialize<IEnumerable<QsNamespace>>(reader);
            }
        }

        /// Given a file id assigned by the Q# compiler, computes the corresponding path in the specified output folder. 
        /// Returns the computed absolute path for a file with the specified ending. 
        /// If the content for that file is specified, writes that content to disk. 
        /// Throws an ArgumentException if the given file id is incompatible with and id assigned by the Q# compiler.
        /// Throws the corresponding exception any of the path operations fails or if the writing fails.  
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
