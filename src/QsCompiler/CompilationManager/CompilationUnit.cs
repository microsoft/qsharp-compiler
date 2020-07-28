﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.Diagnostics;
using Microsoft.Quantum.QsCompiler.ReservedKeywords;
using Microsoft.Quantum.QsCompiler.SymbolManagement;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using static Microsoft.Quantum.QsCompiler.ReservedKeywords.AssemblyConstants;

namespace Microsoft.Quantum.QsCompiler.CompilationBuilder
{
    /// <summary>
    /// this representation will change depending on the binary format in which we ship compiled Q# code
    /// </summary>
    public class References
    {
        public class Headers
        {
            public readonly ImmutableArray<CallableDeclarationHeader> Callables;
            public readonly ImmutableArray<(SpecializationDeclarationHeader, SpecializationImplementation)> Specializations;
            public readonly ImmutableArray<TypeDeclarationHeader> Types;

            internal Headers(
                string source,
                IEnumerable<CallableDeclarationHeader> callables = null,
                IEnumerable<(SpecializationDeclarationHeader, SpecializationImplementation)> specs = null,
                IEnumerable<TypeDeclarationHeader> types = null)
            {
                NonNullable<string> SourceOr(NonNullable<string> origSource) => NonNullable<string>.New(source ?? origSource.Value);
                this.Types = types?.Select(t => t.FromSource(SourceOr(t.SourceFile))).ToImmutableArray() ?? ImmutableArray<TypeDeclarationHeader>.Empty;
                this.Callables = callables?.Select(c => c.FromSource(SourceOr(c.SourceFile))).ToImmutableArray() ?? ImmutableArray<CallableDeclarationHeader>.Empty;
                this.Specializations = specs?.Select(s => (s.Item1.FromSource(SourceOr(s.Item1.SourceFile)), s.Item2 ?? SpecializationImplementation.External)).ToImmutableArray()
                    ?? ImmutableArray<(SpecializationDeclarationHeader, SpecializationImplementation)>.Empty;
            }

            /// <summary>
            /// Initializes a set of reference headers based on the given syntax tree loaded from the specified source.
            /// The source is expected to be the path to the dll from which the syntax has been loaded.
            /// Returns an empty set of headers if the given syntax tree is null.
            /// </summary>
            public Headers(NonNullable<string> source, IEnumerable<QsNamespace> syntaxTree)
                : this(
                    source.Value,
                    syntaxTree?.Callables().Where(c => c.SourceFile.Value.EndsWith(".qs")).Select(CallableDeclarationHeader.New),
                    syntaxTree?.Specializations().Where(c => c.SourceFile.Value.EndsWith(".qs")).Select(s => (SpecializationDeclarationHeader.New(s), s.Implementation)),
                    syntaxTree?.Types().Where(c => c.SourceFile.Value.EndsWith(".qs")).Select(TypeDeclarationHeader.New))
            {
            }

            internal Headers(NonNullable<string> source, IEnumerable<(string, string)> attributes)
                : this(
                    source.Value,
                    CallableHeaders(attributes),
                    SpecializationHeaders(attributes).Select(h => (h, (SpecializationImplementation)null)),
                    TypeHeaders(attributes))
            {
            }
        }

        private static Func<(string, string), string> IsDeclaration(string declarationType) => (attribute) =>
        {
            var (typeName, serialization) = attribute;
            if (!typeName.Equals(declarationType, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }
            return serialization;
        };

        private static IEnumerable<CallableDeclarationHeader> CallableHeaders(IEnumerable<(string, string)> attributes) =>
            attributes.Select(IsDeclaration("CallableDeclarationAttribute")).Where(v => v != null)
                .Select(CallableDeclarationHeader.FromJson).Select(built => built.Item2);

        private static IEnumerable<SpecializationDeclarationHeader> SpecializationHeaders(IEnumerable<(string, string)> attributes) =>
            attributes.Select(IsDeclaration("SpecializationDeclarationAttribute")).Where(v => v != null)
                .Select(SpecializationDeclarationHeader.FromJson).Select(built => built.Item2);

        private static IEnumerable<TypeDeclarationHeader> TypeHeaders(IEnumerable<(string, string)> attributes) =>
            attributes.Select(IsDeclaration("TypeDeclarationAttribute")).Where(v => v != null)
                .Select(TypeDeclarationHeader.FromJson).Select(built => built.Item2);

        /// <summary>
        /// Renames all declarations in the headers for which an alternative name is specified
        /// that may be used when loading a type or callable for testing purposes.
        /// Leaves declarations for which no such name is defined unchanged.
        /// Does not check whether there are any conflicts when using alternative names.
        /// </summary>
        private static Headers LoadTestNames(string source, Headers headers)
        {
            var renaming = headers.Callables.Where(callable => !callable.Kind.IsTypeConstructor)
                .Select(callable => (callable.QualifiedName, callable.Attributes))
                .Concat(headers.Types.Select(type => (type.QualifiedName, type.Attributes)))
                .ToImmutableDictionary(
                    decl => decl.QualifiedName,
                    decl => SymbolResolution.TryGetTestName(decl.Attributes).ValueOr(decl.QualifiedName));

            static QsDeclarationAttribute Renamed(QsQualifiedName originalName, Tuple<int, int> declLocation)
            {
                var attName = new UserDefinedType(
                    NonNullable<string>.New(GeneratedAttributes.Namespace),
                    NonNullable<string>.New(GeneratedAttributes.LoadedViaTestNameInsteadOf),
                    QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>.Null);
                var attArg = SyntaxGenerator.StringLiteral(
                    NonNullable<string>.New(originalName.ToString()),
                    ImmutableArray<TypedExpression>.Empty);
                return new QsDeclarationAttribute(
                    QsNullable<UserDefinedType>.NewValue(attName),
                    attArg,
                    declLocation,
                    QsComments.Empty);
            }

            var rename = new RenameReferences(renaming);
            var types = headers.Types
                .Select(type =>
                    renaming.TryGetValue(type.QualifiedName, out var newName) && !type.QualifiedName.Equals(newName) && type.Location.IsValue // TODO: we should instead fully support auto-generated attributes
                    ? type.AddAttribute(Renamed(type.QualifiedName, type.Location.Item.Offset))
                    : type)
                .Select(rename.OnTypeDeclarationHeader);
            var callables = headers.Callables
                .Select(callable =>
                    renaming.TryGetValue(callable.QualifiedName, out var newName) && !callable.QualifiedName.Equals(newName) && callable.Location.IsValue // TODO: we should instead fully support auto-generated attributes
                    ? callable.AddAttribute(Renamed(callable.QualifiedName, callable.Location.Item.Offset))
                    : callable)
                .Select(rename.OnCallableDeclarationHeader);
            var specializations = headers.Specializations.Select(
                specialization => (rename.OnSpecializationDeclarationHeader(specialization.Item1),
                                   rename.Namespaces.OnSpecializationImplementation(specialization.Item2)));
            return new Headers(source, callables, specializations, types);
        }

        /// <summary>
        /// Checks whether the given sequence of elements contains multiple items with the same qualified name.
        /// Returns a sequence of two strings, with the first one containing the name of the duplication,
        /// and the second one listing all sources in which it occurs.
        /// Returns null if the given sequence of elements is null.
        /// </summary>
        private static IEnumerable<(string, string)> GenerateDiagnosticsForConflicts(IEnumerable<(QsQualifiedName Name, NonNullable<string> Source, AccessModifier Access)> elements) =>
            elements?.Where(e => Namespace.IsDeclarationAccessible(false, e.Access))
                     .GroupBy(e => e.Name)
                     .Where(g => g.Count() != 1)
                     .Select(g => (g.Key, string.Join(", ", g.Select(e => e.Source.Value))))
                     .Select(c => ($"{c.Key.Namespace.Value}.{c.Key.Name.Value}", c.Item2));

        /// <summary>
        /// Dictionary that maps the id of a referenced assembly (given by its location on disk) to the headers defined in that assembly.
        /// </summary>
        public readonly ImmutableDictionary<NonNullable<string>, Headers> Declarations;

        public static References Empty =
            new References(ImmutableDictionary<NonNullable<string>, Headers>.Empty);

        /// <summary>
        /// Combines the current references with the given references, and verifies that there are no conflicts.
        /// Calls the given Action onError with suitable diagnostics if two or more references conflict,
        /// i.e. if two or more references contain a declaration with the same fully qualified name.
        /// Throws an ArgumentNullException if the given dictionary of references is null.
        /// Throws an ArgumentException if the given set shares references with the current one.
        /// </summary>
        internal References CombineWith(References other, Action<ErrorCode, string[]> onError = null)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }
            if (this.Declarations.Keys.Intersect(other.Declarations.Keys).Any())
            {
                throw new ArgumentException("common references exist");
            }
            return new References(this.Declarations.AddRange(other.Declarations), onError: onError);
        }

        /// <summary>
        /// Returns a new collection with the given reference and all its entries removed.
        /// Verifies that there are no conflicts for the new set of references.
        /// Calls the given Action onError with suitable diagnostics if two or more references conflict,
        /// i.e. if two or more references contain a declaration with the same fully qualified name.
        /// Throws an ArgumentNullException if the given diagnostics are null.
        /// </summary>
        internal References Remove(NonNullable<string> source, Action<ErrorCode, string[]> onError = null) =>
            new References(this.Declarations.Remove(source), onError: onError);

        /// <summary>
        /// Given a dictionary that maps the ids of dll files to the corresponding headers,
        /// initializes a new set of references based on the given headers and verifies that there are no conflicts.
        /// Calls the given Action onError with suitable diagnostics if two or more references conflict,
        /// i.e. if two or more references contain a declaration with the same fully qualified name.
        /// If loadTestNames is set to true, then public types and callables declared in referenced assemblies
        /// are exposed via their test name defined by the corresponding attribute.
        /// Throws an ArgumentNullException if the given dictionary of references is null.
        /// </summary>
        public References(ImmutableDictionary<NonNullable<string>, Headers> refs, bool loadTestNames = false, Action<ErrorCode, string[]> onError = null)
        {
            this.Declarations = refs ?? throw new ArgumentNullException(nameof(refs));
            if (loadTestNames)
            {
                this.Declarations = this.Declarations
                    .ToImmutableDictionary(
                        reference => reference.Key,
                        reference => LoadTestNames(reference.Key.Value, reference.Value));
            }

            if (onError == null)
            {
                return;
            }
            var conflicting = new List<(string, string)>();
            var callables = this.Declarations.Values.SelectMany(r => r.Callables).Select(c => (c.QualifiedName, c.SourceFile, c.Modifiers.Access));
            var types = this.Declarations.Values.SelectMany(r => r.Types).Select(t => (t.QualifiedName, t.SourceFile, t.Modifiers.Access));
            conflicting.AddRange(GenerateDiagnosticsForConflicts(callables));
            conflicting.AddRange(GenerateDiagnosticsForConflicts(types));

            foreach (var (name, conflicts) in conflicting.Distinct())
            {
                onError?.Invoke(ErrorCode.ConflictInReferences, new[] { name, conflicts });
            }
        }

        /// <summary>
        /// Combines the syntax trees loaded from different source assemblies and combines them into a single syntax tree.
        /// The first item in the given arguments is expected to contain the id of the source from which the syntax tree was loaded,
        /// and the second item is expected to contain the loaded syntax tree.
        /// The source file of a declaration in the combined tree will be set to the specified source from which it was loaded,
        /// and internal declaration as well as their usages will be renamed to avoid conflicts.
        /// </summary>
        /// <returns>Returns true and the combined syntax tree as out parameter
        /// if the given syntax trees do not contain any conflicting declarations and were successfully combined.
        /// Returns false and an empty array of namespaces as out parameter otherwise.</returns>
        /// <param name="additionalAssemblies">The number of additional assemblies included in the compilation besides the loaded assemblies.</param>
        /// <param name="onError">Invoked on the error messages generated when the given syntax trees contain conflicting declarations.</param>
        /// <param name="loaded">A parameter array of tuples containing the syntax trees to combine
        /// as well as the sources from which they were loaded.</param>
        public static bool CombineSyntaxTrees(
            out ImmutableArray<QsNamespace> combined,
            int additionalAssemblies = 0,
            Action<ErrorCode, string[]> onError = null,
            params (NonNullable<string>, ImmutableArray<QsNamespace>)[] loaded)
        {
            combined = ImmutableArray<QsNamespace>.Empty;
            if (loaded == null)
            {
                return false;
            }

            var (callables, types) = CompilationUnit.RenameInternalDeclarations(
                loaded.SelectMany(loaded => loaded.Item2.Callables().Select(c =>
                    c.WithSourceFile(loaded.Item1)
                    .WithSpecializations(specs => specs.Select(s => s.WithSourceFile(loaded.Item1)).ToImmutableArray()))),
                loaded.SelectMany(loaded => loaded.Item2.Types().Select(t =>
                    t.WithSourceFile(loaded.Item1))),
                additionalAssemblies: additionalAssemblies);

            var conflicting = new List<(string, string)>();
            var callableElems = callables.Select(c => (c.FullName, c.SourceFile, c.Modifiers.Access));
            var typeElems = types.Select(t => (t.FullName, t.SourceFile, t.Modifiers.Access));
            conflicting.AddRange(GenerateDiagnosticsForConflicts(callableElems));
            conflicting.AddRange(GenerateDiagnosticsForConflicts(typeElems));
            foreach (var (name, conflicts) in conflicting.Distinct())
            {
                onError?.Invoke(ErrorCode.ConflictInReferences, new[] { name, conflicts });
            }

            if (conflicting.Any())
            {
                return false;
            }
            combined = CompilationUnit.NewSyntaxTree(callables, types);
            return true;
        }
    }

    /// <summary>
    /// Class representing a compilation;
    /// apart from storing and providing the means to update the compilation itself,
    /// it stores referenced content and provides the infrastructure to track global symbols.
    /// IMPORTANT: The responsiblity to update the compilation to match changes to the GlobalSymbols lays within the the managing entity.
    /// </summary>
    public class CompilationUnit : IReaderWriterLock, IDisposable
    {
        internal References Externals { get; private set; }

        internal NamespaceManager GlobalSymbols { get; private set; }

        private readonly Dictionary<QsQualifiedName, QsCallable> compiledCallables;
        private readonly Dictionary<QsQualifiedName, QsCustomType> compiledTypes;

        private readonly ReaderWriterLockSlim syncRoot;
        private readonly HashSet<ReaderWriterLockSlim> dependentLocks;

        internal readonly RuntimeCapabilities RuntimeCapabilities;
        internal readonly bool IsExecutable;
        internal readonly NonNullable<string> ProcessorArchitecture;

        /// <inheritdoc/>
        public void Dispose()
        {
            this.syncRoot.Dispose();
        }

        /// <summary>
        /// Returns a new CompilationUnit to store and update a compilation referencing the given content (if any),
        /// with the given sequence of locks registered as dependent locks if the sequence is not null.
        /// Throws an ArgumentNullException if any of the given locks is.
        /// </summary>
        internal CompilationUnit(
            RuntimeCapabilities capabilities,
            bool isExecutable,
            NonNullable<string> processorArchitecture,
            References externals = null,
            IEnumerable<ReaderWriterLockSlim> dependentLocks = null)
        {
            this.syncRoot = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            this.dependentLocks = dependentLocks == null
                ? new HashSet<ReaderWriterLockSlim>()
                : new HashSet<ReaderWriterLockSlim>(dependentLocks);
            if (dependentLocks?.Contains(null) ?? false)
            {
                throw new ArgumentNullException(nameof(dependentLocks), "one or more of the given locks is null");
            }

            this.RuntimeCapabilities = capabilities;
            this.IsExecutable = isExecutable;
            this.ProcessorArchitecture = processorArchitecture;

            this.compiledCallables = new Dictionary<QsQualifiedName, QsCallable>();
            this.compiledTypes = new Dictionary<QsQualifiedName, QsCustomType>();
            this.UpdateReferences(externals ?? References.Empty);
        }

        /// <summary>
        /// Replaces the GlobalSymbols to match the newly specified references.
        /// Throws an ArgumentNullException if the given references are null.
        /// </summary>
        internal void UpdateReferences(References externals)
        {
            this.EnterWriteLock();
            try
            {
                this.Externals = externals ?? throw new ArgumentNullException(nameof(externals));
                this.GlobalSymbols = new NamespaceManager(
                    this,
                    this.Externals.Declarations.Values.SelectMany(h => h.Callables),
                    this.Externals.Declarations.Values.SelectMany(h => h.Specializations.Select(t => new Tuple<SpecializationDeclarationHeader, SpecializationImplementation>(t.Item1, t.Item2))),
                    this.Externals.Declarations.Values.SelectMany(h => h.Types),
                    this.RuntimeCapabilities,
                    this.IsExecutable);
            }
            finally
            {
                this.ExitWriteLock();
            }
        }

        /// <summary>
        /// Registers the given lock as a dependent lock -
        /// i.e. whenever both this compilation unit and a dependent lock are required,
        /// ensures that the compilation unit has to be the outer lock.
        /// Throws an ArgumentNullException if the given lock is null.
        /// </summary>
        internal void RegisterDependentLock(ReaderWriterLockSlim depLock)
        {
            #if DEBUG
            if (depLock == null)
            {
                throw new ArgumentNullException(nameof(depLock));
            }
            this.syncRoot.EnterWriteLock();
            try
            {
                lock (this.dependentLocks)
                {
                    this.dependentLocks.Add(depLock);
                }
            }
            finally
            {
                this.syncRoot.ExitWriteLock();
            }
            #endif
        }

        /// <summary>
        /// Removes the given lock from the set of dependent locks.
        /// Returns true if the lock was successfully removed and false otherwise.
        /// Throws an ArgumentNullException if the given lock is null.
        /// </summary>
        internal void UnregisterDependentLock(ReaderWriterLockSlim depLock)
        {
            #if DEBUG
            if (depLock == null)
            {
                throw new ArgumentNullException(nameof(depLock));
            }
            this.syncRoot.EnterWriteLock();
            try
            {
                lock (this.dependentLocks)
                {
                    this.dependentLocks.Remove(depLock);
                }
            }
            finally
            {
                this.syncRoot.ExitWriteLock();
            }
            #endif
        }

        // routines replacing the direct access to the sync root

        /// <summary>
        /// Enters a read-lock, provided none of the dependent locks is set, or a compilation lock is aready held.
        /// Throws an InvalidOperationException if any of the dependent locks is set, but the SyncRoot is not at least read-lock-held.
        /// </summary>
        public void EnterReadLock()
        {
            #if DEBUG
            lock (this.dependentLocks)
            {
                if (this.dependentLocks.Any(l => l.IsAtLeastReadLockHeld()) && !this.syncRoot.IsAtLeastReadLockHeld())
                {
                    throw new InvalidOperationException("cannot enter read lock when a dependent lock is active");
                }
            }
            #endif
            this.syncRoot.EnterReadLock();
        }

        /// <inheritdoc/>
        public void ExitReadLock() => this.syncRoot.ExitReadLock();

        /// <summary>
        /// Enters an upgradeable read-lock, provided none of the dependent locks is set, or a suitable compilation lock is aready held.
        /// Throws an InvalidOperationException if any of the dependent locks is set, but the SyncRoot is not at least read-lock-held.
        /// </summary>
        public void EnterUpgradeableReadLock()
        {
            #if DEBUG
            lock (this.dependentLocks)
            {
                if (this.dependentLocks.Any(l => l.IsAtLeastReadLockHeld()) && !this.syncRoot.IsAtLeastReadLockHeld())
                {
                    throw new InvalidOperationException("cannot enter upgradeable read lock when a dependent lock is active");
                }
            }
            #endif
            this.syncRoot.EnterUpgradeableReadLock();
        }

        public void ExitUpgradeableReadLock() => this.syncRoot.ExitUpgradeableReadLock();

        /// <summary>
        /// Enters a write-lock, provided none of the dependent locks is set, or a suitable compilation lock is aready held.
        /// Throws an InvalidOperationException if any of the dependent locks is set, but the SyncRoot is not at least read-lock-held.
        /// </summary>
        public void EnterWriteLock()
        {
            #if DEBUG
            lock (this.dependentLocks)
            {
                if (this.dependentLocks.Any(l => l.IsAtLeastReadLockHeld()) && !this.syncRoot.IsAtLeastReadLockHeld())
                {
                    throw new InvalidOperationException("cannot enter write lock when a dependent lock is active");
                }
            }
            #endif
            this.syncRoot.EnterWriteLock();
        }

        /// <inheritdoc/>
        public void ExitWriteLock() => this.syncRoot.ExitWriteLock();

        // methods related to accessing and managing information about the compilation

        /// <summary>
        /// Returns all currently compiled Q# callables as ReadOnlyDictionary.
        /// -> Note that the wrapped dictionary may change!
        /// </summary>
        internal IReadOnlyDictionary<QsQualifiedName, QsCallable> GetCallables()
        {
            this.syncRoot.EnterReadLock();
            try
            {
                return new ReadOnlyDictionary<QsQualifiedName, QsCallable>(this.compiledCallables);
            }
            finally
            {
                this.syncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns all currently compiled Q# types as ReadOnlyDictionary.
        /// -> Note that the wrapped dictionary may change!
        /// </summary>
        internal IReadOnlyDictionary<QsQualifiedName, QsCustomType> GetTypes()
        {
            this.syncRoot.EnterReadLock();
            try
            {
                return new ReadOnlyDictionary<QsQualifiedName, QsCustomType>(this.compiledTypes);
            }
            finally
            {
                this.syncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// If the given updates are not null, replaces the contained types in the compilation, or adds them if they do not yet exist.
        /// Proceeds to remove any types that are not currently listed in GlobalSymbols from the compilation, and updates all position information.
        /// Throws an ArgumentNullException if any of the given types to update is null.
        /// </summary>
        internal void UpdateTypes(IEnumerable<QsCustomType> updates)
        {
            this.syncRoot.EnterWriteLock();
            try
            {
                if (updates != null)
                {
                    foreach (var t in updates)
                    {
                        this.compiledTypes[t.FullName] = t ?? throw new ArgumentNullException(nameof(updates), "the given compiled type is null");
                    }
                }

                // remove all types that are no listed in GlobalSymbols

                var currentlyDefined = this.GlobalSymbols.DefinedTypes().ToImmutableDictionary(decl => decl.QualifiedName);
                var keys = this.compiledTypes.Keys.ToImmutableArray();
                foreach (var typeName in keys)
                {
                    if (!currentlyDefined.ContainsKey(typeName))
                    {
                        this.compiledTypes.Remove(typeName);
                    }
                }

                // update the position information for the remaining types

                // NOTE: type constructors are *callables* and hence there may be a temporary discrepancy
                // between the declared types and the corresponding constructors that needs to be resolved before building

                foreach (var declaration in currentlyDefined)
                {
                    var (fullName, header) = (declaration.Key, declaration.Value);
                    var compilationExists = this.compiledTypes.TryGetValue(fullName, out QsCustomType compiled);
                    if (!compilationExists)
                    {
                        continue; // may happen if a file has been modified during global type checking
                    }

                    var type = new QsCustomType(
                        compiled.FullName,
                        compiled.Attributes,
                        compiled.Modifiers,
                        compiled.SourceFile,
                        header.Location,
                        compiled.Type,
                        compiled.TypeItems,
                        compiled.Documentation,
                        compiled.Comments);
                    this.compiledTypes[fullName] = type;
                }
            }
            finally
            {
                this.syncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// If the given updates are not null, replaces the contained callables in the compilation, or adds them if they do not yet exist.
        /// Proceeds to remove any callables and specializations that are not currently listed in GlobalSymbols from the compilation,
        /// and updates the position information for all callables and specializations.
        /// Throws an ArgumentNullException if any of the given callables to update is null.
        /// </summary>
        internal void UpdateCallables(IEnumerable<QsCallable> updates)
        {
            this.syncRoot.EnterWriteLock();
            try
            {
                foreach (var c in updates ?? Array.Empty<QsCallable>())
                {
                    if (c?.Specializations == null || c.Specializations.Contains(null))
                    {
                        throw new ArgumentNullException(nameof(updates), "the given compiled callable or specialization is null");
                    }
                    this.compiledCallables[c.FullName] = c;
                }

                // remove all types and callables that are not listed in GlobalSymbols

                var currentlyDefined = this.GlobalSymbols.DefinedCallables().ToImmutableDictionary(decl => decl.QualifiedName);
                var keys = this.compiledCallables.Keys.ToImmutableArray();
                foreach (var callableName in keys)
                {
                    if (!currentlyDefined.ContainsKey(callableName))
                    {
                        this.compiledCallables.Remove(callableName);
                    } // todo: needs adaption if we want to support external specializations
                }

                // update the position information for the remaining types and callables, keeping only the specializations that are listed in GlobalSymbols

                // NOTE: It indeed needs to be possible that a compilation is still missing for an entry that is listed in GlobalSymbols.
                // Rather than a discrepancy, this is the case when a file has been modified during global type checking -
                // in that case the global type checking does not yield a valid compilation for everything,
                // but still needs to be able to update the compilations that are valid. The same goes for specializations.

                foreach (var declaration in currentlyDefined)
                {
                    var (fullName, header) = (declaration.Key, declaration.Value);
                    if (header.Kind.IsTypeConstructor)
                    {
                        var defaultSpec = new QsSpecialization(
                            QsSpecializationKind.QsBody,
                            header.QualifiedName,
                            header.Attributes,
                            header.SourceFile,
                            header.Location,
                            QsNullable<ImmutableArray<ResolvedType>>.Null,
                            header.Signature,
                            SpecializationImplementation.Intrinsic,
                            ImmutableArray<string>.Empty,
                            QsComments.Empty);
                        this.compiledCallables[fullName] = new QsCallable(
                            header.Kind,
                            header.QualifiedName,
                            header.Attributes,
                            header.Modifiers,
                            header.SourceFile,
                            header.Location,
                            header.Signature,
                            header.ArgumentTuple,
                            ImmutableArray.Create(defaultSpec),
                            header.Documentation,
                            QsComments.Empty);
                        continue;
                    }

                    var compilationExists = this.compiledCallables.TryGetValue(fullName, out QsCallable compiled);
                    if (!compilationExists)
                    {
                        continue; // may happen if a file has been modified during global type checking
                    }

                    // TODO: this needs adaption if we want to support type specializations
                    var specializations = this.GlobalSymbols.DefinedSpecializations(header.QualifiedName).Select(defined =>
                    {
                        var specHeader = defined.Item2;
                        var compiledSpecs = compiled.Specializations.Where(spec =>
                            spec.Kind == specHeader.Kind &&
                            spec.TypeArguments.Equals(specHeader.TypeArguments));
                        QsCompilerError.Verify(compiledSpecs.Count() <= 1, "more than one specialization of the same kind exists");
                        if (!compiledSpecs.Any())
                        {
                            return null; // may happen if a file has been modified during global type checking
                        }

                        var compiledSpec = compiledSpecs.Single();
                        return new QsSpecialization(
                            compiledSpec.Kind,
                            compiledSpec.Parent,
                            compiledSpec.Attributes,
                            compiledSpec.SourceFile,
                            specHeader.Location,
                            compiledSpec.TypeArguments,
                            compiledSpec.Signature,
                            compiledSpec.Implementation,
                            compiledSpec.Documentation,
                            compiledSpec.Comments);
                    })
                    .Where(spec => spec != null).ToImmutableArray();

                    var callable = new QsCallable(
                        compiled.Kind,
                        compiled.FullName,
                        compiled.Attributes,
                        compiled.Modifiers,
                        compiled.SourceFile,
                        header.Location,
                        compiled.Signature,
                        compiled.ArgumentTuple,
                        specializations,
                        compiled.Documentation,
                        compiled.Comments);
                    this.compiledCallables[fullName] = callable;
                }
            }
            finally
            {
                this.syncRoot.ExitWriteLock();
            }
        }

        /// <summary>
        /// Constructs a suitable callable for a given callable declaration header -
        /// i.e. given all information about a callable except the implementation of its specializations,
        /// constructs a QsCallable with the implementation of each specialization set to External.
        /// Throws an ArgumentNullException if the given header is null.
        /// </summary>
        private QsCallable GetImportedCallable(CallableDeclarationHeader header)
        {
            // TODO: this needs to be adapted if we want to support external specializations
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            if (Namespace.IsDeclarationAccessible(false, header.Modifiers.Access))
            {
                var definedSpecs = this.GlobalSymbols.DefinedSpecializations(header.QualifiedName);
                QsCompilerError.Verify(
                    definedSpecs.Length == 0,
                    "external specializations are currently not supported");
            }

            var specializations =
                this.GlobalSymbols
                .ImportedSpecializations(header.QualifiedName)
                .Where(specialization =>
                    // Either the callable is externally accessible, or all of its specializations must be defined in
                    // the same reference as the callable.
                    Namespace.IsDeclarationAccessible(false, header.Modifiers.Access) ||
                    specialization.Item1.SourceFile.Equals(header.SourceFile))
                .Select(specialization =>
                {
                    var (specHeader, implementation) = specialization;
                    var specSignature = specHeader.Kind.IsQsControlled || specHeader.Kind.IsQsControlledAdjoint
                        ? SyntaxGenerator.BuildControlled(header.Signature)
                        : header.Signature;
                    return new QsSpecialization(
                        specHeader.Kind,
                        header.QualifiedName,
                        specHeader.Attributes,
                        specHeader.SourceFile,
                        specHeader.Location,
                        specHeader.TypeArguments,
                        specSignature,
                        implementation,
                        specHeader.Documentation,
                        QsComments.Empty);
                })
                .ToImmutableArray();
            return new QsCallable(
                header.Kind,
                header.QualifiedName,
                header.Attributes,
                header.Modifiers,
                header.SourceFile,
                header.Location,
                header.Signature,
                header.ArgumentTuple,
                specializations,
                header.Documentation,
                QsComments.Empty);
        }

        /// <summary>
        /// Constructs a suitable type for a given type declaration header.
        /// Throws an ArgumentNullException if the given header is null.
        /// </summary>
        private QsCustomType GetImportedType(TypeDeclarationHeader header)
        {
            if (header == null)
            {
                throw new ArgumentNullException(nameof(header));
            }
            return new QsCustomType(
                header.QualifiedName,
                header.Attributes,
                header.Modifiers,
                header.SourceFile,
                header.Location,
                header.Type,
                header.TypeItems,
                header.Documentation,
                QsComments.Empty);
        }

        /// <summary>
        /// Builds a syntax tree containing the given callables and types,
        /// and attaches the documentation specified by the given dictionary - if any - to each namespace.
        /// All elements within a namespace will be sorted in alphabetical order.
        /// Throws an ArgumentNullException if the given callables or types are null.
        /// </summary>
        public static ImmutableArray<QsNamespace> NewSyntaxTree(
            IEnumerable<QsCallable> callables,
            IEnumerable<QsCustomType> types,
            IReadOnlyDictionary<NonNullable<string>, ILookup<NonNullable<string>, ImmutableArray<string>>> documentation = null)
        {
            if (callables == null)
            {
                throw new ArgumentNullException(nameof(callables));
            }
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }
            var emptyLookup = Array.Empty<NonNullable<string>>().ToLookup(ns => ns, _ => ImmutableArray<string>.Empty);

            static string QualifiedName(QsQualifiedName fullName) => $"{fullName.Namespace.Value}.{fullName.Name.Value}";
            static string ElementName(QsNamespaceElement e) =>
                e is QsNamespaceElement.QsCustomType t ? QualifiedName(t.Item.FullName) :
                e is QsNamespaceElement.QsCallable c ? QualifiedName(c.Item.FullName) : null;
            var namespaceElements = callables.Select(c => (c.FullName.Namespace, QsNamespaceElement.NewQsCallable(c)))
                .Concat(types.Select(t => (t.FullName.Namespace, QsNamespaceElement.NewQsCustomType(t))));
            return namespaceElements
                .ToLookup(element => element.Item1, element => element.Item2)
                .Select(elements => new QsNamespace(
                    elements.Key,
                    elements.OrderBy(ElementName).ToImmutableArray(),
                    documentation != null && documentation.TryGetValue(elements.Key, out var doc) ? doc : emptyLookup))
                .ToImmutableArray();
        }

        /// <summary>
        /// Returns the built Q# compilation reflecting the current internal state.
        /// Note that functor generation directives are *not* evaluated in the the returned compilation,
        /// and the returned compilation may contain invalid parts.
        /// Throws an InvalidOperationException if a callable definition is listed in GlobalSymbols for which no compilation exists.
        /// </summary>
        public QsCompilation Build()
        {
            this.syncRoot.EnterReadLock();
            try
            {
                // verify that a compilation indeed exists for each type, callable and specialization currently defined in global symbols

                foreach (var declaration in this.GlobalSymbols.DefinedTypes())
                {
                    var compilationExists = this.compiledTypes.TryGetValue(declaration.QualifiedName, out QsCustomType compiled);
                    if (!compilationExists)
                    {
                        throw new InvalidOperationException($"missing compilation for type " +
                        $"{declaration.QualifiedName.Namespace.Value}.{declaration.QualifiedName.Name.Value} defined in '{declaration.SourceFile.Value}'");
                    }
                }

                var entryPoints = ImmutableArray.CreateBuilder<QsQualifiedName>();
                foreach (var declaration in this.GlobalSymbols.DefinedCallables())
                {
                    if (declaration.Attributes.Any(BuiltIn.MarksEntryPoint))
                    {
                        entryPoints.Add(declaration.QualifiedName);
                    }

                    var compilationExists = this.compiledCallables.TryGetValue(declaration.QualifiedName, out QsCallable compiled);
                    if (!compilationExists)
                    {
                        throw new InvalidOperationException($"missing compilation for callable " +
                        $"{declaration.QualifiedName.Namespace.Value}.{declaration.QualifiedName.Name.Value} defined in '{declaration.SourceFile.Value}'");
                    }

                    foreach (var (_, specHeader) in this.GlobalSymbols.DefinedSpecializations(declaration.QualifiedName))
                    {
                        var compiledSpecs = compiled.Specializations.Where(spec => spec.Kind == specHeader.Kind);
                        QsCompilerError.Verify(compiledSpecs.Count() <= 1, "more than one specialization of the same kind exists"); // currently not supported
                        if (!compiledSpecs.Any())
                        {
                            throw new InvalidOperationException($"missing compilation for specialization " +
                            $"{specHeader.Kind} of {specHeader.Parent.Namespace.Value}.{specHeader.Parent.Name.Value} in '{specHeader.SourceFile.Value}'");
                        }
                    }
                }

                // build the syntax tree
                var callables = this.compiledCallables.Values.Concat(this.GlobalSymbols.ImportedCallables().Select(this.GetImportedCallable));
                var types = this.compiledTypes.Values.Concat(this.GlobalSymbols.ImportedTypes().Select(this.GetImportedType));
                // Rename imported internal declarations by tagging them with their source file to avoid potentially
                // having duplicate names in the syntax tree.
                var (taggedCallables, taggedTypes) = RenameInternalDeclarations(callables, types, predicate: source => this.Externals.Declarations.ContainsKey(source));
                var tree = NewSyntaxTree(taggedCallables, taggedTypes, this.GlobalSymbols.Documentation());
                return new QsCompilation(tree, entryPoints.ToImmutable());
            }
            finally
            {
                this.syncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Returns a look-up that contains the names of all namespaces and the corresponding short hand, if any,
        /// imported within a certain source file for the given namespace.
        /// Throws an ArgumentException if no namespace with the given name exists.
        /// </summary>
        public ILookup<NonNullable<string>, (NonNullable<string>, string)> GetOpenDirectives(NonNullable<string> nsName)
        {
            this.syncRoot.EnterReadLock();
            try
            {
                return this.GlobalSymbols.OpenDirectives(nsName);
            }
            finally
            {
                this.syncRoot.ExitReadLock();
            }
        }

        /// <summary>
        /// Determines the closest preceding specialization for the given position in the given file.
        /// Returns the name of the parent callable, its position in the file as well as the position of the relevant specialization as out parameters.
        /// Returns null without setting any of the out parameters if the given file or position is null, or if the parent callable could not be determined.
        /// Sets the correct namespace name and callable position but returns no implementation if the given position is within a callable declaration.
        /// </summary>
        internal QsScope TryGetSpecializationAt(
            FileContentManager file,
            Position pos,
            out QsQualifiedName callableName,
            out Position callablePos,
            out Position specializationPos)
        {
            (callableName, callablePos, specializationPos) = (null, null, null);
            if (file == null || pos == null || !Utils.IsValidPosition(pos, file))
            {
                return null;
            }

            var nsName = file.TryGetNamespaceAt(pos);
            if (nsName == null)
            {
                return null;
            }
            var root = file.TryGetClosestSpecialization(pos);
            if (root == null)
            {
                return null;
            }

            var ((cName, cPos), (specKind, sPos)) = root.Value;
            (callablePos, specializationPos) = (cPos, sPos);
            callableName = new QsQualifiedName(NonNullable<string>.New(nsName), cName);

            QsSpecialization GetSpecialization(QsQualifiedName fullName, QsSpecializationKind kind)
            {
                if (kind == null || fullName == null)
                {
                    return null;
                }
                if (!this.GetCallables().TryGetValue(fullName, out var qsCallable))
                {
                    return null;
                }
                var compiled = qsCallable.Specializations.Where(spec => spec.Kind.Equals(kind));
                QsCompilerError.Verify(compiled.Count() <= 1, "currently expecting at most one specialization per kind");
                return compiled.SingleOrDefault();
            }
            QsSpecialization relevantSpecialization = GetSpecialization(callableName, specKind);
            if (relevantSpecialization == null || sPos == null || !relevantSpecialization.Implementation.IsProvided)
            {
                return null;
            }

            QsCompilerError.Verify(sPos?.IsSmallerThanOrEqualTo(pos) ?? true, "computed closes preceding specialization does not precede the position in question");
            QsCompilerError.Verify(sPos != null || relevantSpecialization == null, "the position offset should not be null unless the relevant specialization is");
            return ((SpecializationImplementation.Provided)relevantSpecialization.Implementation).Item2;
        }

        /// <summary>
        /// Given all locally defined symbols within a particular specialization of a callable,
        /// returns a new set of LocalDeclarations with the position information updated to the absolute values,
        /// assuming the given positions for the parent callable and the specialization the symbols are defined in are correct.
        /// If no LocalDeclarations are given or the given declarations are null,
        /// returns all (valid) symbols defined as part of the declaration of the parent callable with their position information set to the absolute value.
        /// Returns an empty set of declarations if the name of the parent callable is null or no callable with the name is currently compiled.
        /// </summary>
        internal LocalDeclarations PositionedDeclarations(QsQualifiedName parentCallable, Position callablePos, Position specPos, LocalDeclarations declarations = null)
        {
            LocalDeclarations TryGetLocalDeclarations()
            {
                if (!this.GetCallables().TryGetValue(parentCallable, out var qsCallable))
                {
                    return LocalDeclarations.Empty;
                }
                var definedVars = SyntaxGenerator.ExtractItems(qsCallable.ArgumentTuple).ValidDeclarations();
                return new LocalDeclarations(definedVars);
            }

            if (parentCallable == null)
            {
                return LocalDeclarations.Empty;
            }
            declarations ??= TryGetLocalDeclarations();

            Tuple<int, int> AbsolutePosition(QsNullable<Tuple<int, int>> relOffset) =>
                relOffset.IsNull
                    ? DiagnosticTools.AsTuple(callablePos)
                    : DiagnosticTools.AsTuple(specPos.Add(DiagnosticTools.AsPosition(relOffset.Item)));
            return declarations.WithAbsolutePosition(AbsolutePosition);
        }

        /// <summary>
        /// Returns all locally declared symbols at the given (absolute) position in the given file
        /// and sets the out parameter to the name of the parent callable at that position,
        /// assuming that the position corresponds to a piece of code within the given file.
        /// If includeDeclaredAtPosition is set to true, then this includes the symbols declared within the statement at the specified position,
        /// even if those symbols are *not* visible after the statement ends (e.g. for-loops or qubit allocations).
        /// Note that if the given position does not correspond to a piece of code but rather to whitespace possibly after a scope ending,
        /// the returned declarations or the set parent name are not necessarily accurate - they are for any actual piece of code, though.
        /// If the given file or position is null, or if the locally declared symbols could not be determined, returns an empty LocalDeclarations object.
        /// Sets the parent name to null, if no parent could be determind.
        /// </summary>
        internal LocalDeclarations TryGetLocalDeclarations(FileContentManager file, Position pos, out QsQualifiedName parentCallable, bool includeDeclaredAtPosition = false)
        {
            var implementation = this.TryGetSpecializationAt(file, pos, out parentCallable, out var callablePos, out var specPos);
            var declarations = implementation?.LocalDeclarationsAt(pos.Subtract(specPos), includeDeclaredAtPosition);
            return this.PositionedDeclarations(parentCallable, callablePos, specPos, declarations);
        }

        /// <summary>
        /// Tags the names of internal callables and types that are from a source that satisfies
        /// the given predicate with a unique identifier based on the path to their source,
        /// so that they do not conflict with public callables and types.
        /// If no predicate is specified or the given predicate is null, tags all types and callables.
        /// Renames all usages to the tagged names.
        /// </summary>
        /// <param name="callables">The callables to rename and update if they are internal.</param>
        /// <param name="types">The types to rename and update if they are internal.</param>
        /// <param name="additionalAssemblies">The number of additional assemblies included in the compilation
        /// besides the ones listed as sources in the given types and callables.</param>
        /// <param name="predicate">If specified, only types and callables from a source for which
        /// this function returns true are renamed.</param>
        /// <returns>The renamed and updated callables and types.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the given callables or types are null.</exception>
        internal static (IEnumerable<QsCallable>, IEnumerable<QsCustomType>) RenameInternalDeclarations(
            IEnumerable<QsCallable> callables,
            IEnumerable<QsCustomType> types,
            int additionalAssemblies = 0,
            Func<NonNullable<string>, bool> predicate = null)
        {
            if (callables == null)
            {
                throw new ArgumentNullException(nameof(callables));
            }
            if (types == null)
            {
                throw new ArgumentNullException(nameof(types));
            }
            predicate ??= _ => true;

            // Assign a unique ID to each reference.

            var decorator = new NameDecorator($"QsRef");
            var ids =
                callables.Select(callable => callable.SourceFile.Value)
                .Concat(types.Select(type => type.SourceFile.Value))
                .Distinct()
                .Where(source => predicate(NonNullable<string>.New(source)))
                // this setup will mean that internal declarations won't get replaced with target specific implementations
                .Select((source, idx) => (source, idx))
                // we need an id here that is uniquely associated with a source name
                // to ensure that internal names are unique even when this is not called on the entire compilation
                .ToImmutableDictionary(entry => entry.source, entry => entry.idx + additionalAssemblies);

            ImmutableDictionary<QsQualifiedName, QsQualifiedName> GetMappingForSourceGroup(
                IGrouping<string, (QsQualifiedName Name, string Source, AccessModifier Access)> group) =>
                group
                .Where(item =>
                    !Namespace.IsDeclarationAccessible(false, item.Access) &&
                    predicate(NonNullable<string>.New(item.Source)))
                .ToImmutableDictionary(
                    item => item.Name,
                    item => decorator.Decorate(item.Name, ids[item.Source]));

            // rename all internal declarations and their usages

            var transformations =
                callables.Select(callable =>
                    (name: callable.FullName, source: callable.SourceFile.Value, access: callable.Modifiers.Access))
                .Concat(types.Select(type =>
                    (name: type.FullName, source: type.SourceFile.Value, access: type.Modifiers.Access)))
                .GroupBy(item => item.source)
                .ToImmutableDictionary(
                    group => group.Key,
                    group => new RenameReferences(GetMappingForSourceGroup(group)));

            var taggedCallables = callables.Select(
                callable => transformations[callable.SourceFile.Value].Namespaces.OnCallableDeclaration(callable))
                .ToImmutableArray();
            var taggedTypes = types.Select(
                type => transformations[type.SourceFile.Value].Namespaces.OnTypeDeclaration(type))
                .ToImmutableArray();
            return (taggedCallables, taggedTypes);
        }
    }
}
