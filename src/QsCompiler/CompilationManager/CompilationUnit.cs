// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SymbolManagement;
using Microsoft.Quantum.QsCompiler.SyntaxProcessing;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.VisualStudio.LanguageServer.Protocol;


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
            public readonly ImmutableArray<SpecializationDeclarationHeader> Specializations;
            public readonly ImmutableArray<TypeDeclarationHeader> Types;

            public Headers(
                IEnumerable<CallableDeclarationHeader> callables = null, 
                IEnumerable<SpecializationDeclarationHeader> specs = null, 
                IEnumerable<TypeDeclarationHeader> types = null)
            {
                this.Callables = callables?.ToImmutableArray() ?? ImmutableArray<CallableDeclarationHeader>.Empty;
                this.Specializations = specs?.ToImmutableArray() ?? ImmutableArray<SpecializationDeclarationHeader>.Empty;
                this.Types = types?.ToImmutableArray() ?? ImmutableArray<TypeDeclarationHeader>.Empty; 
            }
        }

        /// <summary>
        /// the keys are the file ids (location of the dll file) and the values are the headers defined in that file
        /// </summary>
        public readonly ImmutableDictionary<NonNullable<string>, Headers> Declarations; 

        public static References Empty = 
            new References(Enumerable.Empty<KeyValuePair<NonNullable<string>, ImmutableArray<(string, string)>>>(), out var _);

        /// <summary>
        /// Throws an ArgumentNullException if the given argument is null. 
        /// Throws an ArgumentException if the given set shares references with the current one. 
        /// </summary>
        internal References CombineWith(References other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));
            if (this.Declarations.Keys.Intersect(other.Declarations.Keys).Any()) throw new ArgumentException("common references exist");
            return new References (this.Declarations.AddRange(other.Declarations)); 
        }

        /// <summary>
        /// Returns a new collection with the given reference and all its entries removed. 
        /// </summary>
        internal References Remove(NonNullable<string> source) =>
            new References(this.Declarations.Remove(source));

        /// <summary>
        /// NOTE: Does not do any verification of the arguments whatsoever and hence needs to remain private!
        /// IMPORTANT: this class relies on the fact tha the corresponding references need to be listed for all declaration headers!
        /// </summary>
        private References(ImmutableDictionary<NonNullable<string>, Headers> refs) =>
            this.Declarations = refs;

        /// <summary>
        /// NOTE: Does not do any verification of the arguments and hence needs to remain private!
        /// Throws an ArgumentException if the absolute file path for a given Uri cannot be determined. 
        /// Throws the corresponding exception if the given argument are null or cannot be processed. 
        /// </summary>
        private References(IEnumerable<KeyValuePair<NonNullable<string>, ImmutableArray<(string, string)>>> attributes, 
            out ImmutableHashSet<NonNullable<string>> serializationErrors)
        {
            Func<(string, string), string> IsDeclaration(string declarationType) => (attribute) =>
            {
                var (typeName, serialization) = attribute;
                if (!typeName.Equals(declarationType, StringComparison.InvariantCulture)) return null;
                return serialization;
            };
            
            var callableAttr = attributes.Select(kv => (kv.Key, kv.Value.Select(IsDeclaration("CallableDeclarationAttribute")).Where(v => v != null)));
            var specsAttr = attributes.Select(kv => (kv.Key, kv.Value.Select(IsDeclaration("SpecializationDeclarationAttribute")).Where(v => v != null)));                
            var typeAttr = attributes.Select(kv => (kv.Key, kv.Value.Select(IsDeclaration("TypeDeclarationAttribute")).Where(v => v != null)));

            var errs = new HashSet<NonNullable<string>>(); 
            (NonNullable<string>, T) Build<T>(Func<string, Tuple<bool, T>> builder, string arg, NonNullable<string> source)
            {
                var (success, built) = builder(arg);
                if (!success) errs.Add(source);
                return (source, built); 
            }

            var callables = callableAttr
                .SelectMany(kv => kv.Item2.Select(v => Build(CallableDeclarationHeader.FromJson, v, kv.Item1)))
                .ToLookup(kv => kv.Item1, kv => kv.Item2.FromSource(kv.Item1)); 
            var specializations = specsAttr
                .SelectMany(kv => kv.Item2.Select(v => Build(SpecializationDeclarationHeader.FromJson, v, kv.Item1)))
                .ToLookup(kv => kv.Item1, kv => kv.Item2.FromSource(kv.Item1));
            var types = typeAttr
                .SelectMany(kv => kv.Item2.Select(v => Build(TypeDeclarationHeader.FromJson, v, kv.Item1)))
                .ToLookup(kv => kv.Item1, kv => kv.Item2.FromSource(kv.Item1));

            serializationErrors = errs.ToImmutableHashSet(); 
            this.Declarations = attributes.ToImmutableDictionary(
                a => a.Key, 
                a => new Headers(callables[a.Key], specializations[a.Key], types[a.Key]  )
            ); 
        }

        /// <summary>
        /// Returns the built references as well as the ids of all files with attributes that could not be deserialized as out parameter. 
        /// Throws an ArgumentNullException if the given attributes are null. 
        /// </summary>
        internal static bool TryInitializeFrom(
            IEnumerable<KeyValuePair<Uri, ImmutableArray<(string, string)>>> attributes,
            out References references, out IEnumerable<Uri> serializationErrors)
        {
            if (attributes == null) throw new ArgumentNullException(nameof(attributes));
            NonNullable<string> GetId(Uri uri) =>
                CompilationUnitManager.TryGetFileId(uri, out var id) ?
                    id : throw new ArgumentException("expecting an absolute file uri for all references");
            var sources = attributes.ToImmutableDictionary(kv => GetId(kv.Key));
            var items = sources.Select(kv => new KeyValuePair<NonNullable<string>, ImmutableArray<(string, string)>>(kv.Key, kv.Value.Value));

            ImmutableHashSet<NonNullable<string>> errs = null; 
            (references, serializationErrors) = (References.Empty, ImmutableHashSet<Uri>.Empty); 
            try { references = new References(items, out errs); }
            catch { return false; }
            serializationErrors = errs.Select(id => sources[id].Key);
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
        private readonly Dictionary<QsQualifiedName, QsCallable> CompiledCallables;
        private readonly Dictionary<QsQualifiedName, QsCustomType> CompiledTypes;
        private readonly ReaderWriterLockSlim SyncRoot;
        private readonly HashSet<ReaderWriterLockSlim> DependentLocks;

        public void Dispose()
        { this.SyncRoot.Dispose(); }

        /// <summary>
        /// Returns a new CompilationUnit to store and update a compilation referencing the given content (if any),
        /// with the given sequence of locks registered as dependent locks if the sequence is not null.
        /// Throws an ArgumentNullException if any of the given locks is.
        /// </summary>
        internal CompilationUnit(References externals = null, IEnumerable<ReaderWriterLockSlim> dependentLocks = null)
        {
            this.SyncRoot = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            this.DependentLocks = dependentLocks == null
                ? new HashSet<ReaderWriterLockSlim>()
                : new HashSet<ReaderWriterLockSlim>(dependentLocks);
            if (dependentLocks?.Contains(null) ?? false) throw new ArgumentNullException(nameof(dependentLocks), "one or more of the given locks is null");
            this.CompiledCallables = new Dictionary<QsQualifiedName, QsCallable>();
            this.CompiledTypes = new Dictionary<QsQualifiedName, QsCustomType>();
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
                this.GlobalSymbols = new NamespaceManager(this, 
                    this.Externals.Declarations.Values.SelectMany(h => h.Callables), 
                    this.Externals.Declarations.Values.SelectMany(h => h.Specializations), 
                    this.Externals.Declarations.Values.SelectMany(h => h.Types));
            }
            finally { this.ExitWriteLock(); }
        }

        /// <summary>
        /// Registers the given lock as a dependent lock - 
        /// i.e. whenever both this compilation unit and a dependent lock are required, 
        /// ensures that the compilation unit has to be the outer lock.
        /// Throws an ArgumentNullException if the given lock is null.
        /// </summary>
        internal void RegisterDependentLock(ReaderWriterLockSlim depLock)
        {
            if (depLock == null) throw new ArgumentNullException(nameof(depLock));
            this.SyncRoot.EnterWriteLock();
            try
            {
                lock (this.DependentLocks)
                { this.DependentLocks.Add(depLock); }
            }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        /// <summary>
        /// Removes the given lock from the set of dependent locks.
        /// Returns true if the lock was successfully removed and false otherwise.
        /// Throws an ArgumentNullException if the given lock is null.
        /// </summary>
        internal void UnregisterDependentLock(ReaderWriterLockSlim depLock)
        {
            if (depLock == null) throw new ArgumentNullException(nameof(depLock));
            this.SyncRoot.EnterWriteLock();
            try
            {
                lock (this.DependentLocks)
                { this.DependentLocks.Remove(depLock); }
            }
            finally { this.SyncRoot.ExitWriteLock(); }
        }


        // routines replacing the direct access to the sync root

        /// <summary>
        /// Enters a read-lock, provided none of the dependent locks is set, or a compilation lock is aready held. 
        /// Throws an InvalidOperationException if any of the dependent locks is set, but the SyncRoot is not at least read-lock-held.
        /// </summary>
        public void EnterReadLock()
        {
            #if DEBUG
            lock (this.DependentLocks)
            {
                if (this.DependentLocks.Any(l => l.IsAtLeastReadLockHeld()) && !this.SyncRoot.IsAtLeastReadLockHeld())
                { throw new InvalidOperationException("cannot enter read lock when a dependent lock is active"); }
            }
            #endif
            this.SyncRoot.EnterReadLock();
        }
        public void ExitReadLock() => this.SyncRoot.ExitReadLock();

        /// <summary>
        /// Enters an upgradeable read-lock, provided none of the dependent locks is set, or a suitable compilation lock is aready held. 
        /// Throws an InvalidOperationException if any of the dependent locks is set, but the SyncRoot is not at least read-lock-held.
        /// </summary>
        public void EnterUpgradeableReadLock()
        {
            #if DEBUG
            lock (this.DependentLocks)
            {
                if (this.DependentLocks.Any(l => l.IsAtLeastReadLockHeld()) && !this.SyncRoot.IsAtLeastReadLockHeld())
                { throw new InvalidOperationException("cannot enter upgradeable read lock when a dependent lock is active"); }
            }
            #endif
            this.SyncRoot.EnterUpgradeableReadLock();
        }
        public void ExitUpgradeableReadLock() => this.SyncRoot.ExitUpgradeableReadLock();

        /// <summary>
        /// Enters a write-lock, provided none of the dependent locks is set, or a suitable compilation lock is aready held. 
        /// Throws an InvalidOperationException if any of the dependent locks is set, but the SyncRoot is not at least read-lock-held.
        /// </summary>
        public void EnterWriteLock()
        {
            #if DEBUG
            lock (this.DependentLocks)
            {
                if (this.DependentLocks.Any(l => l.IsAtLeastReadLockHeld()) && !this.SyncRoot.IsAtLeastReadLockHeld())
                { throw new InvalidOperationException("cannot enter write lock when a dependent lock is active"); }
            }
            #endif
            this.SyncRoot.EnterWriteLock();
        }
        public void ExitWriteLock() => this.SyncRoot.ExitWriteLock();


        // methods related to accessing and managing information about the compilation

        /// <summary>
        /// Returns all currently compiled Q# callables as ReadOnlyDictionary.
        /// -> Note that the wrapped dictionary may change!
        /// </summary>
        internal IReadOnlyDictionary<QsQualifiedName, QsCallable> GetCallables()
        {
            this.SyncRoot.EnterReadLock();
            try { return new ReadOnlyDictionary<QsQualifiedName, QsCallable>(this.CompiledCallables); }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        /// <summary>
        /// Returns all currently compiled Q# types as ReadOnlyDictionary.
        /// -> Note that the wrapped dictionary may change!
        /// </summary>
        internal IReadOnlyDictionary<QsQualifiedName, QsCustomType> GetTypes()
        {
            this.SyncRoot.EnterReadLock();
            try { return new ReadOnlyDictionary<QsQualifiedName, QsCustomType>(this.CompiledTypes); }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        /// <summary>
        /// If the given updates are not null, replaces the contained types in the compilation, or adds them if they do not yet exist.
        /// Proceeds to remove any types that are not currently listed in GlobalSymbols from the compilation, and updates all position information. 
        /// Throws an ArgumentNullException if any of the given types to update is null.
        /// </summary>
        internal void UpdateTypes(IEnumerable<QsCustomType> updates)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                if (updates != null)
                {
                    foreach (var t in updates)
                    {  this.CompiledTypes[t.FullName] = t  ?? throw new ArgumentNullException(nameof(updates), "the given compiled type is null"); }
                }

                // remove all types that are no listed in GlobalSymbols

                var currentlyDefined = this.GlobalSymbols.DefinedTypes().ToImmutableDictionary(decl => decl.QualifiedName);
                var keys = this.CompiledTypes.Keys.ToImmutableArray();
                foreach (var typeName in keys)
                {
                    if (!currentlyDefined.ContainsKey(typeName))
                    { this.CompiledTypes.Remove(typeName); }
                }

                // update the position information for the remaining types

                // NOTE: type constructors are *callables* and hence there may be a temporary discrepancy 
                // between the declared types and the corresponding constructors that needs to be resolved before building

                foreach (var declaration in currentlyDefined)
                {
                    var (fullName, header) = (declaration.Key, declaration.Value);
                    var compilationExists = this.CompiledTypes.TryGetValue(fullName, out QsCustomType compiled);
                    if (!compilationExists) continue; // may happen if a file has been modified during global type checking

                    var location = new QsLocation(header.Position, header.SymbolRange);
                    var type = new QsCustomType(compiled.FullName, compiled.SourceFile, location, compiled.Type, compiled.TypeItems, compiled.Documentation, compiled.Comments);
                    this.CompiledTypes[fullName] = type;
                }
            }
            finally { this.SyncRoot.ExitWriteLock(); }

        }

        /// <summary>
        /// If the given updates are not null, replaces the contained callables in the compilation, or adds them if they do not yet exist.
        /// Proceeds to remove any callables and specializations that are not currently listed in GlobalSymbols from the compilation, 
        /// and updates the position information for all callables and specializations. 
        /// Throws an ArgumentNullException if any of the given callables to update is null.
        /// </summary>
        internal void UpdateCallables(IEnumerable<QsCallable> updates)
        {
            this.SyncRoot.EnterWriteLock();
            try
            {
                foreach (var c in updates ?? new QsCallable[0])
                {
                    if (c?.Specializations == null || c.Specializations.Contains(null))
                    { throw new ArgumentNullException(nameof(updates), "the given compiled callable or specialization is null"); }
                    this.CompiledCallables[c.FullName] = c;
                }

                // remove all types and callables that are not listed in GlobalSymbols

                var currentlyDefined = this.GlobalSymbols.DefinedCallables().ToImmutableDictionary(decl => decl.QualifiedName);
                var keys = this.CompiledCallables.Keys.ToImmutableArray();
                foreach (var callableName in keys)
                {
                    if (!currentlyDefined.ContainsKey(callableName))
                    { this.CompiledCallables.Remove(callableName); }  // todo: needs adaption once we have external specializations
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
                        var specLocation = new QsLocation(header.Position, header.SymbolRange);
                        var defaultSpec = new QsSpecialization(QsSpecializationKind.QsBody, header.QualifiedName, header.SourceFile, specLocation, 
                            QsNullable<ImmutableArray<ResolvedType>>.Null, header.Signature, SpecializationImplementation.Intrinsic, ImmutableArray<string>.Empty, QsComments.Empty);
                        this.CompiledCallables[fullName] = new QsCallable(header.Kind, header.QualifiedName, header.SourceFile, specLocation,
                            header.Signature, header.ArgumentTuple, ImmutableArray.Create<QsSpecialization>(defaultSpec), header.Documentation, QsComments.Empty);
                        continue;
                    }

                    var compilationExists = this.CompiledCallables.TryGetValue(fullName, out QsCallable compiled);
                    if (!compilationExists) continue; // may happen if a file has been modified during global type checking

                    // TODO: this needs adaption once we fully support type specializations
                    var specializations = this.GlobalSymbols.DefinedSpecializations(header.QualifiedName).Select(defined =>
                    {
                        var specHeader = defined.Item2;
                        var compiledSpecs = compiled.Specializations.Where(spec => spec.Kind == specHeader.Kind);
                        QsCompilerError.Verify(compiledSpecs.Count() <= 1, "more than one specialization of the same kind exists"); // currently not supported
                        if (!compiledSpecs.Any()) return null; // may happen if a file has been modified during global type checking

                        var compiledSpec = compiledSpecs.Single();
                        var specLocation = new QsLocation(specHeader.Position, specHeader.HeaderRange);
                        return new QsSpecialization(compiledSpec.Kind, compiledSpec.Parent, compiledSpec.SourceFile, specLocation,
                            compiledSpec.TypeArguments, compiledSpec.Signature, compiledSpec.Implementation, compiledSpec.Documentation, compiledSpec.Comments); 
                    })
                    .Where(spec => spec != null).ToImmutableArray();

                    var location = new QsLocation(header.Position, header.SymbolRange);
                    var callable = new QsCallable(compiled.Kind, compiled.FullName, compiled.SourceFile, location, 
                        compiled.Signature, compiled.ArgumentTuple, specializations, compiled.Documentation, compiled.Comments); 
                    this.CompiledCallables[fullName] = callable;
                }
            }
            finally { this.SyncRoot.ExitWriteLock(); }
        }

        /// <summary>
        /// Constructs a suitable callable for a given callable declaration header - 
        /// i.e. given all information about a callable except the implementation of its specializations, 
        /// constructs a QsCallable with the implementation of each specialization set to External. 
        /// Throws an ArgumentNullException if the given header is null.
        /// </summary>
        public QsCallable GetImportedCallable(CallableDeclarationHeader header)
        {
            // TODO: this needs to be adapted once we support external specializations
            if (header == null) throw new ArgumentNullException(nameof(header));
            var importedSpecs = this.GlobalSymbols.ImportedSpecializations(header.QualifiedName);
            var definedSpecs = this.GlobalSymbols.DefinedSpecializations(header.QualifiedName).Select(defined => defined.Item2);
            var specializations = importedSpecs.Concat(definedSpecs).Select(specHeader =>
            {
                var specLocation = new QsLocation(specHeader.Position, specHeader.HeaderRange);
                var specSignature = specHeader.Kind.IsQsControlled || specHeader.Kind.IsQsControlledAdjoint 
                    ? SyntaxGenerator.BuildControlled(header.Signature) 
                    : header.Signature;
                return new QsSpecialization(specHeader.Kind, header.QualifiedName, specHeader.SourceFile, specLocation, 
                    specHeader.TypeArguments, specSignature, SpecializationImplementation.External, specHeader.Documentation, QsComments.Empty);
            })
            .ToImmutableArray();
            var location = new QsLocation(header.Position, header.SymbolRange);
            return new QsCallable(header.Kind, header.QualifiedName, header.SourceFile, location, 
                header.Signature, header.ArgumentTuple, specializations, header.Documentation, QsComments.Empty);
        }

        /// <summary>
        /// Constructs a suitable type for a given type declaration header. 
        /// Throws an ArgumentNullException if the given header is null.
        /// </summary>
        public QsCustomType GetImportedType(TypeDeclarationHeader header)
        {
            if (header == null) throw new ArgumentNullException(nameof(header));
            var location = new QsLocation(header.Position, header.SymbolRange);
            return new QsCustomType(header.QualifiedName, header.SourceFile, location, header.Type, header.TypeItems, header.Documentation, QsComments.Empty);
        }

        /// <summary>
        /// Returns the syntax tree for the current state of the compilation as out parameter.
        /// Note that functor generation directives are *not* evaluated in the the returned tree,
        /// and the returned tree may contain invalid parts. 
        /// Throws an InvalidOperationException if a callable definition is listed in GlobalSymbols for which no compilation exists. 
        /// </summary>
        public ImmutableArray<QsNamespace> Build()
        {
            this.SyncRoot.EnterReadLock();
            try
            {
                // verify that a compilation indeed exists for each type, callable and specialization currently defined in global symbols

                foreach (var declaration in this.GlobalSymbols.DefinedTypes())
                {
                    var compilationExists = this.CompiledTypes.TryGetValue(declaration.QualifiedName, out QsCustomType compiled);
                    if (!compilationExists) throw new InvalidOperationException($"missing compilation for type " +
                        $"{declaration.QualifiedName.Namespace.Value}.{declaration.QualifiedName.Name.Value} defined in '{declaration.SourceFile.Value}'");
                }

                foreach (var declaration in this.GlobalSymbols.DefinedCallables())
                {
                    var compilationExists = this.CompiledCallables.TryGetValue(declaration.QualifiedName, out QsCallable compiled);
                    if (!compilationExists) throw new InvalidOperationException($"missing compilation for callable " +
                        $"{declaration.QualifiedName.Namespace.Value}.{declaration.QualifiedName.Name.Value} defined in '{declaration.SourceFile.Value}'");

                    foreach (var (_,specHeader) in this.GlobalSymbols.DefinedSpecializations(declaration.QualifiedName))
                    {
                        var compiledSpecs = compiled.Specializations.Where(spec => spec.Kind == specHeader.Kind);
                        QsCompilerError.Verify(compiledSpecs.Count() <= 1, "more than one specialization of the same kind exists"); // currently not supported
                        if (!compiledSpecs.Any()) throw new InvalidOperationException($"missing compilation for specialization " +
                            $"{specHeader.Kind} of {specHeader.Parent.Namespace.Value}.{specHeader.Parent.Name.Value} in '{specHeader.SourceFile.Value}'");
                    }
                }

                // build the syntax tree

                (NonNullable<string>, QsNamespaceElement) GetNamespaceElementForImportedCallable(CallableDeclarationHeader header) =>
                    (header.QualifiedName.Namespace, QsNamespaceElement.NewQsCallable(this.GetImportedCallable(header)));
                (NonNullable<string>, QsNamespaceElement) GetNamespaceElementForImportedType(TypeDeclarationHeader header) =>
                    (header.QualifiedName.Namespace, QsNamespaceElement.NewQsCustomType(this.GetImportedType(header)));

                var definedCallables = this.CompiledCallables.Select(callable => (callable.Key.Namespace, QsNamespaceElement.NewQsCallable(callable.Value)));
                var importedCallables = this.GlobalSymbols.ImportedCallables().Select(imported => GetNamespaceElementForImportedCallable(imported));
                var definedTypes = this.CompiledTypes.Select(type => (type.Key.Namespace, QsNamespaceElement.NewQsCustomType(type.Value)));
                var importedTypes = this.GlobalSymbols.ImportedTypes().Select(imported => GetNamespaceElementForImportedType(imported));

                var namespaceElements = definedCallables.Concat(importedCallables).Concat(definedTypes).Concat(importedTypes);
                var documentation = this.GlobalSymbols.Documentation();
                return namespaceElements
                    .ToLookup(element => element.Item1, element => element.Item2)
                    .Select(elements => new QsNamespace(elements.Key, elements.ToImmutableArray(), documentation[elements.Key]))
                    .ToImmutableArray();
            }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        /// <summary>
        /// Returns a look-up that contains the names of all namespaces imported within a certain source file for the given namespace.
        /// Throws an ArgumentException if no namespace with the given name exists. 
        /// </summary>
        public ILookup<NonNullable<string>, (NonNullable<string>, string)> GetOpenDirectives(NonNullable<string> nsName)
        {
            this.SyncRoot.EnterReadLock();
            try { return this.GlobalSymbols.OpenDirectives(nsName); }
            finally { this.SyncRoot.ExitReadLock(); }
        }

        /// <summary>
        /// Determines the closest preceding specialization for the given position in the given file. 
        /// Returns the name of the parent callable, its position in the file as well as the position of the relevant specialization as out parameters. 
        /// Returns null without setting any of the out parameters if the given file or position is null, or if the parent callable could not be determined. 
        /// Sets the correct namespace name and callable position but returns no implementation if the given position is within a callable declaration.
        /// </summary>
        internal QsScope TryGetSpecializationAt(FileContentManager file, Position pos, 
            out QsQualifiedName callableName, out Position callablePos, out Position specializationPos)
        {
            (callableName, callablePos, specializationPos) = (null, null, null);
            if (file == null || pos == null || !Utils.IsValidPosition(pos, file)) return null;

            var nsName = file.TryGetNamespaceAt(pos);
            if (nsName == null) return null;
            var root = file.TryGetClosestSpecialization(pos);
            if (root == null) return null;

            var ((cName, cPos), (specKind, sPos)) = root.Value;
            (callablePos, specializationPos) = (cPos, sPos); 
            callableName = new QsQualifiedName(NonNullable<string>.New(nsName), cName);

            QsSpecialization GetSpecialization(QsQualifiedName fullName, QsSpecializationKind kind)
            {
                if (kind == null || fullName == null) return null;
                if (!this.GetCallables().TryGetValue(fullName, out var qsCallable)) return null;
                var compiled = qsCallable.Specializations.Where(spec => spec.Kind.Equals(kind));
                QsCompilerError.Verify(compiled.Count() <= 1, "currently expecting at most one specialization per kind");
                return compiled.SingleOrDefault();
            }
            QsSpecialization relevantSpecialization = GetSpecialization(callableName, specKind);
            if (relevantSpecialization == null || sPos == null || !relevantSpecialization.Implementation.IsProvided) return null;

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
                if (!this.GetCallables().TryGetValue(parentCallable, out var qsCallable)) return LocalDeclarations.Empty;
                var definedVars = SyntaxGenerator.ExtractItems(qsCallable.ArgumentTuple).ValidDeclarations();
                return new LocalDeclarations(definedVars);
            }

            if (parentCallable == null) return LocalDeclarations.Empty;
            declarations = declarations ?? TryGetLocalDeclarations();

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
        /// Note that if the given position does not correspond to a piece of code but rather to whitespace possibly after a scope ending,
        /// the returned declarations or the set parent name are not necessarily accurate - they are for any actual piece of code, though. 
        /// If the given file or position is null, or if the locally declared symbols could not be determined, returns an empty LocalDeclarations object. 
        /// Sets the parent name to null, if no parent could be determind.
        /// </summary>
        internal LocalDeclarations TryGetLocalDeclarations(FileContentManager file, Position pos, out QsQualifiedName parentCallable)
        {
            var implementation = this.TryGetSpecializationAt(file, pos, out parentCallable, out var callablePos, out var specPos);
            var declarations = implementation?.LocalDeclarationsAt(pos.Subtract(specPos)).Item1;
            return this.PositionedDeclarations(parentCallable, callablePos, specPos, declarations); 
        }
    }
}
