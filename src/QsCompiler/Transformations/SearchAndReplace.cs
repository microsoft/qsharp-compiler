// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Quantum.QsCompiler.DataTypes;
using Microsoft.Quantum.QsCompiler.SyntaxTokens;
using Microsoft.Quantum.QsCompiler.SyntaxTree;
using Microsoft.Quantum.QsCompiler.Transformations.BasicTransformations;
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;
using Range = Microsoft.Quantum.QsCompiler.DataTypes.Range;

namespace Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace
{
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;

    // routines for finding occurrences of symbols/identifiers

    /// <summary>
    /// Class that allows to walk the syntax tree and find all locations where a certain identifier occurs.
    /// If a set of source file names is given on initialization, the search is limited to callables and specializations in those files.
    /// </summary>
    public class IdentifierReferences
    : SyntaxTreeTransformation<IdentifierReferences.TransformationState>
    {
        public class Location : IEquatable<Location>
        {
            public readonly string SourceFile;

            /// <summary>
            /// contains the offset of the root node relative to which the statement location is given
            /// </summary>
            public readonly Position DeclarationOffset;

            /// <summary>
            /// contains the location of the statement containing the symbol relative to the root node
            /// </summary>
            public readonly QsLocation RelativeStatementLocation;

            /// <summary>
            /// contains the range of the symbol relative to the statement position
            /// </summary>
            public readonly Range SymbolRange;

            public Location(string source, Position declOffset, QsLocation stmLoc, Range range)
            {
                this.SourceFile = source;
                this.DeclarationOffset = declOffset;
                this.RelativeStatementLocation = stmLoc;
                this.SymbolRange = range;
            }

            /// <inheritdoc/>
            public bool Equals(Location? other) =>
                this.SourceFile == other?.SourceFile
                && this.DeclarationOffset == other?.DeclarationOffset
                && this.RelativeStatementLocation.Offset == other?.RelativeStatementLocation.Offset
                && this.RelativeStatementLocation.Range == other?.RelativeStatementLocation.Range
                && this.SymbolRange == other?.SymbolRange;

            /// <inheritdoc/>
            public override bool Equals(object obj) =>
                this.Equals(obj as Location);

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                var (hash, multiplier) = (0x51ed270b, -1521134295);
                if (this.DeclarationOffset != null)
                {
                    hash = (hash * multiplier) + this.DeclarationOffset.GetHashCode();
                }
                if (this.RelativeStatementLocation.Offset != null)
                {
                    hash = (hash * multiplier) + this.RelativeStatementLocation.Offset.GetHashCode();
                }
                if (this.RelativeStatementLocation.Range != null)
                {
                    hash = (hash * multiplier) + this.RelativeStatementLocation.Range.GetHashCode();
                }
                if (this.SymbolRange != null)
                {
                    hash = (hash * multiplier) + this.SymbolRange.GetHashCode();
                }
                return this.SourceFile == null ? hash : (hash * multiplier) + this.SourceFile.GetHashCode();
            }
        }

        /// <summary>
        /// Class used to track the internal state for a transformation that finds all locations where a certain identifier occurs.
        /// If no source file is specified prior to transformation, its name is set to the empty string.
        /// The DeclarationOffset needs to be set prior to transformation, and in particular after defining a source file.
        /// If no defaultOffset is specified upon initialization then only the locations of occurrences within statements are logged.
        /// </summary>
        public class TransformationState
        {
            public Tuple<string, QsLocation>? DeclarationLocation { get; internal set; }

            public ImmutableHashSet<Location> Locations { get; private set; }

            /// <summary>
            /// Whenever DeclarationOffset is set, the current statement offset is set to this default value.
            /// </summary>
            private readonly QsLocation? defaultOffset = null;
            private readonly IImmutableSet<string>? relevantSourceFiles = null;

            internal bool IsRelevant(string source) =>
                this.relevantSourceFiles?.Contains(source) ?? true;

            internal TransformationState(
                Func<Identifier, bool> trackId,
                QsLocation? defaultOffset = null,
                IImmutableSet<string>? limitToSourceFiles = null)
            {
                this.TrackIdentifier = trackId;
                this.relevantSourceFiles = limitToSourceFiles;
                this.Locations = ImmutableHashSet<Location>.Empty;
                this.defaultOffset = defaultOffset;
            }

            private string currentSourceFile = "";
            private Position? rootOffset = null;
            internal QsLocation? CurrentLocation = null;
            internal readonly Func<Identifier, bool> TrackIdentifier;

            public Position? DeclarationOffset
            {
                internal get => this.rootOffset;
                set
                {
                    this.rootOffset = value;
                    this.CurrentLocation = this.defaultOffset;
                }
            }

            public string Source
            {
                internal get => this.currentSourceFile;
                set
                {
                    this.currentSourceFile = value;
                    this.rootOffset = null;
                    this.CurrentLocation = null;
                }
            }

            internal void LogIdentifierLocation(Identifier id, QsNullable<Range> range)
            {
                if (this.TrackIdentifier(id)
                    && this.CurrentLocation?.Offset != null
                    && range.IsValue
                    && !(this.rootOffset is null))
                {
                    var idLoc = new Location(this.Source, this.rootOffset, this.CurrentLocation, range.Item);
                    this.Locations = this.Locations.Add(idLoc);
                }
            }

            internal void LogIdentifierLocation(TypedExpression ex)
            {
                if (ex.Expression is QsExpressionKind.Identifier id)
                {
                    this.LogIdentifierLocation(id.Item1, ex.Range);
                }
            }
        }

        public IdentifierReferences(TransformationState state)
        : base(state, TransformationOptions.NoRebuild)
        {
            this.Types = new TypeTransformation(this);
            this.Expressions = new TypedExpressionWalker<TransformationState>(this.SharedState.LogIdentifierLocation, this);
            this.Statements = new StatementTransformation(this);
            this.Namespaces = new NamespaceTransformation(this);
        }

        public IdentifierReferences(string idName, QsLocation? defaultOffset, IImmutableSet<string>? limitToSourceFiles = null)
        : this(new TransformationState(id => id is Identifier.LocalVariable varName && varName.Item == idName, defaultOffset, limitToSourceFiles))
        {
        }

        public IdentifierReferences(QsQualifiedName idName, QsLocation? defaultOffset, IImmutableSet<string>? limitToSourceFiles = null)
        : this(new TransformationState(id => id is Identifier.GlobalCallable cName && cName.Item.Equals(idName), defaultOffset, limitToSourceFiles))
        {
        }

        // static methods for convenience

        public static ImmutableHashSet<Location> Find(
            string idName,
            QsScope scope,
            string sourceFile,
            Position rootLoc)
        {
            var finder = new IdentifierReferences(idName, null, ImmutableHashSet.Create(sourceFile));
            finder.SharedState.Source = sourceFile;
            finder.SharedState.DeclarationOffset = rootLoc;
            finder.Statements.OnScope(scope);
            return finder.SharedState.Locations;
        }

        public static ImmutableHashSet<Location> Find(
            QsQualifiedName idName,
            QsNamespace ns,
            QsLocation defaultOffset,
            out Tuple<string, QsLocation>? declarationLocation,
            IImmutableSet<string>? limitToSourceFiles = null)
        {
            var finder = new IdentifierReferences(idName, defaultOffset, limitToSourceFiles);
            finder.Namespaces.OnNamespace(ns);
            declarationLocation = finder.SharedState.DeclarationLocation;
            return finder.SharedState.Locations;
        }

        // helper classes

        private class TypeTransformation
        : TypeTransformation<TransformationState>
        {
            public TypeTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsTypeKind OnUserDefinedType(UserDefinedType udt)
            {
                var id = Identifier.NewGlobalCallable(new QsQualifiedName(udt.Namespace, udt.Name));
                this.SharedState.LogIdentifierLocation(id, udt.Range);
                return QsTypeKind.NewUserDefinedType(udt);
            }

            public override QsTypeKind OnTypeParameter(QsTypeParameter tp)
            {
                var resT = ResolvedType.New(QsTypeKind.NewTypeParameter(tp));
                var id = Identifier.NewLocalVariable(SyntaxTreeToQsharp.Default.ToCode(resT) ?? "");
                this.SharedState.LogIdentifierLocation(id, tp.Range);
                return resT.Resolution;
            }
        }

        private class StatementTransformation
        : StatementTransformation<TransformationState>
        {
            public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsNullable<QsLocation> OnLocation(QsNullable<QsLocation> loc)
            {
                this.SharedState.CurrentLocation = loc.IsValue ? loc.Item : null;
                return loc;
            }
        }

        private class NamespaceTransformation
        : NamespaceTransformation<TransformationState>
        {
            public NamespaceTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsCustomType OnTypeDeclaration(QsCustomType t)
            {
                if (!this.SharedState.IsRelevant(t.Source.AssemblyOrCode) || t.Location.IsNull)
                {
                    return t;
                }
                if (this.SharedState.TrackIdentifier(Identifier.NewGlobalCallable(t.FullName)))
                {
                    this.SharedState.DeclarationLocation = Tuple.Create(t.Source.AssemblyOrCode, t.Location.Item);
                }
                return base.OnTypeDeclaration(t);
            }

            public override QsCallable OnCallableDeclaration(QsCallable c)
            {
                if (!this.SharedState.IsRelevant(c.Source.AssemblyOrCode) || c.Location.IsNull)
                {
                    return c;
                }
                if (this.SharedState.TrackIdentifier(Identifier.NewGlobalCallable(c.FullName)))
                {
                    this.SharedState.DeclarationLocation = Tuple.Create(c.Source.AssemblyOrCode, c.Location.Item);
                }
                return base.OnCallableDeclaration(c);
            }

            public override QsDeclarationAttribute OnAttribute(QsDeclarationAttribute att)
            {
                var declRoot = this.SharedState.DeclarationOffset;
                this.SharedState.DeclarationOffset = att.Offset;
                if (att.TypeId.IsValue)
                {
                    this.Transformation.Types.OnUserDefinedType(att.TypeId.Item);
                }
                this.Transformation.Expressions.OnTypedExpression(att.Argument);
                this.SharedState.DeclarationOffset = declRoot;
                return att;
            }

            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec) =>
                this.SharedState.IsRelevant(spec.Source.AssemblyOrCode) ? base.OnSpecializationDeclaration(spec) : spec;

            public override QsNullable<QsLocation> OnLocation(QsNullable<QsLocation> loc)
            {
                this.SharedState.DeclarationOffset = loc.IsValue ? loc.Item.Offset : null;
                return loc;
            }

            public override Source OnSource(Source source)
            {
                this.SharedState.Source = source.AssemblyOrCode;
                return source;
            }
        }
    }

    // routines for finding all symbols/identifiers

    /// <summary>
    /// Generates a look-up for all used local variables and their location (if available) in any of the transformed
    /// scopes, as well as one for all local variables reassigned in any of the transformed scopes and their locations
    /// (if available).
    /// </summary>
    /// <remarks>
    /// The location information is relative to the root node, i.e. the start position of the containing specialization
    /// declaration.
    /// </remarks>
    public class AccumulateIdentifiers
    : SyntaxTreeTransformation<AccumulateIdentifiers.TransformationState>
    {
        public class TransformationState
        {
            internal QsLocation? StatementLocation = null;
            internal Func<TypedExpression, TypedExpression> UpdatedExpression;

            private readonly List<(string, QsLocation?)> updatedLocals = new List<(string, QsLocation?)>();
            private readonly List<(string, QsLocation?)> usedLocals = new List<(string, QsLocation?)>();

            internal TransformationState() =>
                this.UpdatedExpression = new TypedExpressionWalker<TransformationState>(this.UpdatedLocal, this).OnTypedExpression;

            public ILookup<string, QsLocation?> ReassignedVariables =>
                this.updatedLocals.ToLookup(var => var.Item1, var => var.Item2);

            public ILookup<string, QsLocation?> UsedLocalVariables =>
                this.usedLocals.ToLookup(var => var.Item1, var => var.Item2);

            private Action<TypedExpression> Add(List<(string, QsLocation?)> accumulate) => (TypedExpression ex) =>
            {
                if (ex.Expression is QsExpressionKind.Identifier id &&
                    id.Item1 is Identifier.LocalVariable var)
                {
                    var location = this.StatementLocation is null ? null : new QsLocation(
                        this.StatementLocation.Offset,
                        ex.Range.IsValue ? ex.Range.Item : this.StatementLocation.Range);
                    accumulate.Add((var.Item, location));
                }
            };

            internal Action<TypedExpression> UsedLocal => this.Add(this.usedLocals);

            internal Action<TypedExpression> UpdatedLocal => this.Add(this.updatedLocals);
        }

        public AccumulateIdentifiers()
        : base(new TransformationState(), TransformationOptions.NoRebuild)
        {
            this.Statements = new StatementTransformation(this);
            this.StatementKinds = new StatementKindTransformation(this);
            this.Expressions = new TypedExpressionWalker<TransformationState>(this.SharedState.UsedLocal, this);
            this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
        }

        // helper classes

        private class StatementTransformation
        : StatementTransformation<TransformationState>
        {
            public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsStatement OnStatement(QsStatement stm)
            {
                this.SharedState.StatementLocation = stm.Location.IsNull ? null : stm.Location.Item;
                this.StatementKinds.OnStatementKind(stm.Statement);
                return stm;
            }
        }

        private class StatementKindTransformation
        : StatementKindTransformation<TransformationState>
        {
            public StatementKindTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent, TransformationOptions.NoRebuild)
            {
            }

            public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
            {
                this.SharedState.UpdatedExpression(stm.Lhs);
                this.Expressions.OnTypedExpression(stm.Rhs);
                return QsStatementKind.NewQsValueUpdate(stm);
            }
        }
    }

    // routines for replacing symbols/identifiers

    /// <summary>
    /// Provides simple name decoration (or name mangling) by prefixing names with a label and number.
    /// </summary>
    public class NameDecorator
    {
        private const string Original = "original";

        private readonly string label;

        private readonly Regex pattern;

        /// <summary>
        /// Creates a new name decorator using the label.
        /// </summary>
        /// <param name="label">The label to use as the prefix for decorated names.</param>
        public NameDecorator(string label)
        {
            this.label = label;
            this.pattern = new Regex($"^__{Regex.Escape(label)}_?[0-9]*__(?<{Original}>.*)__$");
        }

        /// <summary>
        /// Decorates the name with the label of this name decorator and the given number.
        /// </summary>
        /// <param name="name">The name to decorate.</param>
        /// <param name="number">The number to use along with the label to decorate the name.</param>
        /// <returns>The decorated name.</returns>
        public string Decorate(string name, int number) =>
            $"__{this.label}{(number < -0 ? "_" : "")}{Math.Abs(number)}__{name}__";

        /// <summary>
        /// Decorates the name of the qualified name with the label of this name decorator and the given number.
        /// </summary>
        /// <param name="name">The qualified name to decorate.</param>
        /// <param name="number">The number to use along with the label to decorate the qualified name.</param>
        /// <returns>The decorated qualified name.</returns>
        public QsQualifiedName Decorate(QsQualifiedName name, int number) =>
            new QsQualifiedName(name.Namespace, this.Decorate(name.Name, number));

        /// <summary>
        /// Reverses decoration previously done to the name using the same label as this name decorator.
        /// </summary>
        /// <param name="name">The decorated name to undecorate.</param>
        /// <returns>
        /// The original name before decoration, if the decorated name uses the same label as this name decorator;
        /// otherwise, null.
        /// </returns>
        public string? Undecorate(string name)
        {
            var match = this.pattern.Match(name).Groups[Original];
            return match.Success ? match.Value : null;
        }
    }

    /// <summary>
    /// Upon transformation, assigns each defined variable a unique name, independent on the scope, and replaces all references to it accordingly.
    /// The original variable name can be recovered by using the static method StripUniqueName.
    /// This class is *not* threadsafe.
    /// </summary>
    public class UniqueVariableNames
    : SyntaxTreeTransformation<UniqueVariableNames.TransformationState>
    {
        private static readonly NameDecorator Decorator = new NameDecorator("qsVar");

        public class TransformationState
        {
            private int variableNr = 0;
            private readonly Dictionary<string, string> uniqueNames = new Dictionary<string, string>();

            internal bool TryGetUniqueName(string name, out string unique) =>
                this.uniqueNames.TryGetValue(name, out unique);

            internal QsExpressionKind AdaptIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
                sym is Identifier.LocalVariable varName && this.uniqueNames.TryGetValue(varName.Item, out var unique)
                    ? QsExpressionKind.NewIdentifier(Identifier.NewLocalVariable(unique), tArgs)
                    : QsExpressionKind.NewIdentifier(sym, tArgs);

            /// <summary>
            /// Will overwrite the dictionary entry mapping a variable name to the corresponding unique name if the key already exists.
            /// </summary>
            internal string GenerateUniqueName(string varName)
            {
                var unique = Decorator.Decorate(varName, this.variableNr++);
                this.uniqueNames[varName] = unique;
                return unique;
            }
        }

        public UniqueVariableNames()
        : base(new TransformationState())
        {
            this.Statements = new StatementTransformation(this);
            this.StatementKinds = new StatementKindTransformation(this);
            this.ExpressionKinds = new ExpressionKindTransformation(this);
            this.Types = new TypeTransformation<TransformationState>(this, TransformationOptions.Disabled);
        }

        // static methods for convenience

        internal static QsQualifiedName PrependGuid(QsQualifiedName original) =>
            new QsQualifiedName(
                original.Namespace,
                "_" + Guid.NewGuid().ToString("N") + "_" + original.Name);

        public static string StripUniqueName(string uniqueName) => Decorator.Undecorate(uniqueName) ?? uniqueName;

        // helper classes

        private class StatementTransformation
        : StatementTransformation<TransformationState>
        {
            public StatementTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent)
            {
            }

            public override string OnVariableName(string name) =>
                this.SharedState.TryGetUniqueName(name, out var unique) ? unique : name;
        }

        private class StatementKindTransformation
        : StatementKindTransformation<TransformationState>
        {
            public StatementKindTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent)
            {
            }

            public override SymbolTuple OnSymbolTuple(SymbolTuple syms) =>
                syms is SymbolTuple.VariableNameTuple tuple
                    ? SymbolTuple.NewVariableNameTuple(tuple.Item.Select(this.OnSymbolTuple).ToImmutableArray())
                    : syms is SymbolTuple.VariableName varName
                    ? SymbolTuple.NewVariableName(this.SharedState.GenerateUniqueName(varName.Item))
                    : syms;
        }

        private class ExpressionKindTransformation
        : ExpressionKindTransformation<TransformationState>
        {
            public ExpressionKindTransformation(SyntaxTreeTransformation<TransformationState> parent)
            : base(parent)
            {
            }

            public override QsExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
                this.SharedState.AdaptIdentifier(sym, tArgs);
        }
    }

    /// <summary>
    /// A transformation that renames all references to each given qualified name.
    /// </summary>
    public class RenameReferences : SyntaxTreeTransformation
    {
        private class TransformationState
        {
            private readonly IImmutableDictionary<QsQualifiedName, QsQualifiedName> names;

            internal TransformationState(IImmutableDictionary<QsQualifiedName, QsQualifiedName> names) =>
                this.names = names;

            /// <summary>
            /// Gets the renamed version of the qualified name if one exists; otherwise, returns the original name.
            /// </summary>
            /// <param name="name">The qualified name to rename.</param>
            /// <returns>
            /// The renamed version of the qualified name if one exists; otherwise, returns the original name.
            /// </returns>
            internal QsQualifiedName GetNewName(QsQualifiedName name) => this.names.GetValueOrDefault(name) ?? name;

            /// <summary>
            /// Gets the renamed version of the user-defined type if one exists; otherwise, returns the original one.
            /// </summary>
            /// <returns>
            /// The renamed version of the user-defined type if one exists; otherwise, returns the original one.
            /// </returns>
            internal UserDefinedType RenameUdt(UserDefinedType udt)
            {
                var newName = this.GetNewName(new QsQualifiedName(udt.Namespace, udt.Name));
                return new UserDefinedType(newName.Namespace, newName.Name, udt.Range);
            }
        }

        private readonly TransformationState state;

        /// <summary>
        /// Creates a new rename references transformation.
        /// </summary>
        /// <param name="names">The mapping from existing names to new names.</param>
        public RenameReferences(IImmutableDictionary<QsQualifiedName, QsQualifiedName> names)
        {
            this.state = new TransformationState(names);
            this.Types = new TypeTransformation(this);
            this.ExpressionKinds = new ExpressionKindTransformation(this);
            this.Namespaces = new NamespaceTransformation(this);
        }

        // methods for transformations on headers

        /// <summary>
        /// Renames references in the callable declaration header, including the name of the callable itself.
        /// </summary>
        /// <param name="callable">The callable declaration header in which to rename references.</param>
        /// <returns>The callable declaration header with renamed references.</returns>
        public CallableDeclarationHeader OnCallableDeclarationHeader(CallableDeclarationHeader callable) =>
            new CallableDeclarationHeader(
                kind: callable.Kind,
                qualifiedName: this.state.GetNewName(callable.QualifiedName),
                attributes: callable.Attributes.Select(this.Namespaces.OnAttribute).ToImmutableArray(),
                modifiers: callable.Modifiers,
                source: callable.Source,
                position: callable.Position,
                symbolRange: callable.SymbolRange,
                argumentTuple: this.Namespaces.OnArgumentTuple(callable.ArgumentTuple),
                signature: this.Namespaces.OnSignature(callable.Signature),
                documentation: this.Namespaces.OnDocumentation(callable.Documentation));

        /// <summary>
        /// Renames references in the specialization declaration header, including the name of the specialization
        /// itself.
        /// </summary>
        /// <param name="specialization">The specialization declaration header in which to rename references.</param>
        /// <returns>The specialization declaration header with renamed references.</returns>
        public SpecializationDeclarationHeader OnSpecializationDeclarationHeader(
            SpecializationDeclarationHeader specialization)
        {
            var typeArguments =
                specialization.TypeArguments.IsValue
                ? QsNullable<ImmutableArray<ResolvedType>>.NewValue(
                    specialization.TypeArguments.Item.Select(this.Types.OnType).ToImmutableArray())
                : QsNullable<ImmutableArray<ResolvedType>>.Null;
            return new SpecializationDeclarationHeader(
                kind: specialization.Kind,
                typeArguments: typeArguments,
                information: specialization.Information,
                parent: this.state.GetNewName(specialization.Parent),
                attributes: specialization.Attributes.Select(this.Namespaces.OnAttribute).ToImmutableArray(),
                source: specialization.Source,
                position: specialization.Position,
                headerRange: specialization.HeaderRange,
                documentation: this.Namespaces.OnDocumentation(specialization.Documentation));
        }

        /// <summary>
        /// Renames references in the type declaration header, including the name of the type itself.
        /// </summary>
        /// <param name="type">The type declaration header in which to rename references.</param>
        /// <returns>The type declaration header with renamed references.</returns>
        public TypeDeclarationHeader OnTypeDeclarationHeader(TypeDeclarationHeader type)
        {
            return new TypeDeclarationHeader(
                qualifiedName: this.state.GetNewName(type.QualifiedName),
                attributes: type.Attributes.Select(this.Namespaces.OnAttribute).ToImmutableArray(),
                modifiers: type.Modifiers,
                source: type.Source,
                position: type.Position,
                symbolRange: type.SymbolRange,
                type: this.Types.OnType(type.Type),
                typeItems: this.Namespaces.OnTypeItems(type.TypeItems),
                documentation: this.Namespaces.OnDocumentation(type.Documentation));
        }

        // private helper classes

        private class TypeTransformation : Core.TypeTransformation
        {
            private readonly TransformationState state;

            public TypeTransformation(RenameReferences parent) : base(parent) =>
                this.state = parent.state;

            public override QsTypeKind OnUserDefinedType(UserDefinedType udt) =>
                QsTypeKind.NewUserDefinedType(this.state.RenameUdt(udt));

            public override QsTypeKind OnTypeParameter(QsTypeParameter tp) =>
                QsTypeKind.NewTypeParameter(new QsTypeParameter(this.state.GetNewName(tp.Origin), tp.TypeName, tp.Range));
        }

        private class ExpressionKindTransformation : Core.ExpressionKindTransformation
        {
            private readonly TransformationState state;

            public ExpressionKindTransformation(RenameReferences parent) : base(parent) =>
                this.state = parent.state;

            public override QsExpressionKind OnIdentifier(Identifier id, QsNullable<ImmutableArray<ResolvedType>> typeArgs)
            {
                if (id is Identifier.GlobalCallable global)
                {
                    id = Identifier.NewGlobalCallable(this.state.GetNewName(global.Item));
                }
                return base.OnIdentifier(id, typeArgs);
            }
        }

        private class NamespaceTransformation : Core.NamespaceTransformation
        {
            private readonly TransformationState state;

            public NamespaceTransformation(RenameReferences parent) : base(parent) =>
                this.state = parent.state;

            public override QsDeclarationAttribute OnAttribute(QsDeclarationAttribute attribute)
            {
                var argument = this.Transformation.Expressions.OnTypedExpression(attribute.Argument);
                var typeId = attribute.TypeId.IsValue
                    ? QsNullable<UserDefinedType>.NewValue(this.state.RenameUdt(attribute.TypeId.Item))
                    : attribute.TypeId;
                return new QsDeclarationAttribute(typeId, argument, attribute.Offset, attribute.Comments);
            }

            public override QsCallable OnCallableDeclaration(QsCallable callable) =>
                base.OnCallableDeclaration(callable.WithFullName(this.state.GetNewName));

            public override QsCustomType OnTypeDeclaration(QsCustomType type) =>
                base.OnTypeDeclaration(type.WithFullName(this.state.GetNewName));

            public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec) =>
                base.OnSpecializationDeclaration(spec.WithParent(this.state.GetNewName));
        }
    }
}
