// Copyright (c) Microsoft Corporation.
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
    public class IdentifierReferences : MonoTransformation
    {
        public class Location : IEquatable<Location>
        {
            public string SourceFile { get; }

            /// <summary>
            /// contains the offset of the root node relative to which the statement location is given
            /// </summary>
            public Position DeclarationOffset { get; }

            /// <summary>
            /// contains the location of the statement containing the symbol relative to the root node
            /// </summary>
            public QsLocation RelativeStatementLocation { get; }

            /// <summary>
            /// contains the range of the symbol relative to the statement position
            /// </summary>
            public Range SymbolRange { get; }

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

        public Tuple<string, QsLocation>? DeclarationLocation { get; internal set; }

        public ImmutableHashSet<Location> Locations { get; private set; }

        /// <summary>
        /// Whenever DeclarationOffset is set, the current statement offset is set to this default value.
        /// </summary>
        private readonly QsLocation? defaultOffset = null;

        private readonly IImmutableSet<string>? relevantSourceFiles = null;

        private bool IsRelevant(string source) =>
            this.relevantSourceFiles?.Contains(source) ?? true;

        private string currentSourceFile = "";
        private Position? rootOffset = null;

        private QsLocation? CurrentLocation { get; set; } = null;

        private Func<Identifier, bool> TrackIdentifier { get; }

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

        private void LogIdentifierLocation(Identifier id, QsNullable<Range> range)
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

        private void LogIdentifierLocation(TypedExpression ex)
        {
            if (ex.Expression is QsExpressionKind.Identifier id)
            {
                this.LogIdentifierLocation(id.Item1, ex.Range);
            }
        }

        /// <summary>
        /// Class used to track the internal state for a transformation that finds all locations where a certain identifier occurs.
        /// If no source file is specified prior to transformation, its name is set to the empty string.
        /// The DeclarationOffset needs to be set prior to transformation, and in particular after defining a source file.
        /// If no defaultOffset is specified upon initialization then only the locations of occurrences within statements are logged.
        /// </summary>
        private IdentifierReferences(
            Func<Identifier, bool> trackId,
            QsLocation? defaultOffset = null,
            IImmutableSet<string>? limitToSourceFiles = null)
            : base(TransformationOptions.NoRebuild)
        {
            this.TrackIdentifier = trackId;
            this.relevantSourceFiles = limitToSourceFiles;
            this.Locations = ImmutableHashSet<Location>.Empty;
            this.defaultOffset = defaultOffset;
        }

        public IdentifierReferences(string idName, QsLocation? defaultOffset, IImmutableSet<string>? limitToSourceFiles = null)
            : this(id => id is Identifier.LocalVariable varName && varName.Item == idName, defaultOffset, limitToSourceFiles)
        {
        }

        public IdentifierReferences(QsQualifiedName idName, QsLocation? defaultOffset, IImmutableSet<string>? limitToSourceFiles = null)
            : this(id => id is Identifier.GlobalCallable cName && cName.Item.Equals(idName), defaultOffset, limitToSourceFiles)
        {
        }

        /* static methods for convenience */

        // TODO: RELEASE 2022-05: Remove IdentifierReferences.Find(..., QsScope, ...).
        [Obsolete("Use IdentifierReferences.FindInScope.")]
        public static ImmutableHashSet<Location> Find(
            string idName, QsScope scope, string sourceFile, Position rootLoc) =>
            FindInScope(sourceFile, rootLoc, scope, idName);

        // TODO: RELEASE 2022-05: Remove IdentifierReferences.Find(..., QsNamespace, ...).
        [Obsolete]
        public static ImmutableHashSet<Location> Find(
            QsQualifiedName idName,
            QsNamespace ns,
            QsLocation defaultOffset,
            out Tuple<string, QsLocation>? declarationLocation,
            IImmutableSet<string>? limitToSourceFiles = null)
        {
            var finder = new IdentifierReferences(idName, defaultOffset, limitToSourceFiles);
            finder.OnNamespace(ns);
            declarationLocation = finder.DeclarationLocation;
            return finder.Locations;
        }

        /// <summary>
        /// Finds references to an identifier in a scope.
        /// </summary>
        /// <param name="file">The source file name.</param>
        /// <param name="specPosition">The position of the specialization that contains the scope.</param>
        /// <param name="scope">The scope.</param>
        /// <param name="ident">The identifier name.</param>
        /// <returns>The locations of the references.</returns>
        public static ImmutableHashSet<Location> FindInScope(
            string file, Position specPosition, QsScope scope, string ident)
        {
            var references = new IdentifierReferences(ident, null, ImmutableHashSet.Create(file));
            references.Source = file;
            references.DeclarationOffset = specPosition;
            references.OnScope(scope);
            return references.Locations;
        }

        /// <summary>
        /// Finds references to an identifier in an expression.
        /// </summary>
        /// <param name="file">The source file name.</param>
        /// <param name="specPosition">The position of the specialization that contains the expression.</param>
        /// <param name="statementLocation">The location of the statement that contains the expression.</param>
        /// <param name="expression">The expression.</param>
        /// <param name="ident">The identifier name.</param>
        /// <returns>The locations of the references.</returns>
        public static ImmutableHashSet<Location> FindInExpression(
            string file,
            Position specPosition,
            QsLocation? statementLocation,
            TypedExpression expression,
            string ident)
        {
            var references = new IdentifierReferences(ident, null, ImmutableHashSet.Create(file));
            references.Source = file;
            references.DeclarationOffset = specPosition;
            references.CurrentLocation = statementLocation;
            references.OnTypedExpression(expression);
            return references.Locations;
        }

        /* overrides */

        public override QsNullable<QsLocation> OnAbsoluteLocation(QsNullable<QsLocation> loc)
        {
            this.DeclarationOffset = loc.IsValue ? loc.Item.Offset : null;
            return loc;
        }

        public override QsNullable<QsLocation> OnRelativeLocation(QsNullable<QsLocation> loc)
        {
            this.CurrentLocation = loc.IsValue ? loc.Item : null;
            return loc;
        }

        public override ResolvedType OnType(ResolvedType type)
        {
            var id = type.Resolution switch
            {
                QsTypeKind.UserDefinedType udt =>
                    Identifier.NewGlobalCallable(new QsQualifiedName(udt.Item.Namespace, udt.Item.Name)),
                QsTypeKind.TypeParameter _ => Identifier.NewLocalVariable(SyntaxTreeToQsharp.Default.ToCode(type)),
                _ => null,
            };

            if (!(id is null) && type.Range is TypeRange.Annotated annotatedRange)
            {
                this.LogIdentifierLocation(id, QsNullable<Range>.NewValue(annotatedRange.Item));
            }

            return base.OnType(type);
        }

        public override QsCustomType OnTypeDeclaration(QsCustomType t)
        {
            if (!this.IsRelevant(t.Source.AssemblyOrCodeFile) || t.Location.IsNull)
            {
                return t;
            }

            if (this.TrackIdentifier(Identifier.NewGlobalCallable(t.FullName)))
            {
                this.DeclarationLocation = Tuple.Create(t.Source.AssemblyOrCodeFile, t.Location.Item);
            }

            return base.OnTypeDeclaration(t);
        }

        public override QsCallable OnCallableDeclaration(QsCallable c)
        {
            if (!this.IsRelevant(c.Source.AssemblyOrCodeFile) || c.Location.IsNull)
            {
                return c;
            }

            if (this.TrackIdentifier(Identifier.NewGlobalCallable(c.FullName)))
            {
                this.DeclarationLocation = Tuple.Create(c.Source.AssemblyOrCodeFile, c.Location.Item);
            }

            return base.OnCallableDeclaration(c);
        }

        public override QsDeclarationAttribute OnAttribute(QsDeclarationAttribute att)
        {
            var declRoot = this.DeclarationOffset;
            this.DeclarationOffset = att.Offset;
            if (att.TypeId.IsValue)
            {
                this.OnUserDefinedType(att.TypeId.Item);
            }

            this.OnTypedExpression(att.Argument);
            this.DeclarationOffset = declRoot;
            return att;
        }

        public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec) =>
            this.IsRelevant(spec.Source.AssemblyOrCodeFile) ? base.OnSpecializationDeclaration(spec) : spec;

        public override Source OnSource(Source source)
        {
            this.Source = source.AssemblyOrCodeFile;
            return source;
        }

        public override TypedExpression OnTypedExpression(TypedExpression ex)
        {
            this.LogIdentifierLocation(ex);
            return base.OnTypedExpression(ex);
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
    public class AccumulateIdentifiers : MonoTransformation
    {
        private QsLocation? StatementLocation { get; set; } = null;

        private bool IsInUpdate { get; set; } = false;

        private readonly List<(string, QsLocation?)> updatedLocals = new List<(string, QsLocation?)>();
        private readonly List<(string, QsLocation?)> usedLocals = new List<(string, QsLocation?)>();

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

        private Action<TypedExpression> UsedLocal => this.Add(this.usedLocals);

        private Action<TypedExpression> UpdatedLocal => this.Add(this.updatedLocals);

        public ILookup<string, QsLocation?> ReassignedVariables =>
                this.updatedLocals.ToLookup(var => var.Item1, var => var.Item2);

        public ILookup<string, QsLocation?> UsedLocalVariables =>
            this.usedLocals.ToLookup(var => var.Item1, var => var.Item2);

        public AccumulateIdentifiers()
            : base(TransformationOptions.NoRebuild)
        {
        }

        /* overrides */

        public override QsStatement OnStatement(QsStatement stm)
        {
            this.StatementLocation = stm.Location.IsNull ? null : stm.Location.Item;
            this.OnStatementKind(stm.Statement);
            return stm;
        }

        public override QsStatementKind OnValueUpdate(QsValueUpdate stm)
        {
            var isInUpdateContext = this.IsInUpdate;
            this.IsInUpdate = true;
            this.OnTypedExpression(stm.Lhs);
            this.IsInUpdate = isInUpdateContext;
            this.OnTypedExpression(stm.Rhs);
            return QsStatementKind.NewQsValueUpdate(stm);
        }

        public override TypedExpression OnTypedExpression(TypedExpression ex)
        {
            if (this.IsInUpdate)
            {
                this.UpdatedLocal(ex);
            }
            else
            {
                this.UsedLocal(ex);
            }

            return base.OnTypedExpression(ex);
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

        /* static methods for name decorations in general */

        private static readonly Regex GUID =
            new Regex(@"^_[({]?[a-fA-F0-9]{8}[-]?([a-fA-F0-9]{4}[-]?){3}[a-fA-F0-9]{12}[})]?_", RegexOptions.IgnoreCase);

        internal static QsQualifiedName PrependGuid(QsQualifiedName original) =>
            new QsQualifiedName(
                original.Namespace,
                "_" + Guid.NewGuid().ToString("N") + "_" + original.Name);

        public static bool IsAutoGeneratedName(QsQualifiedName mangled) =>
            GUID.IsMatch(mangled.Name);

        public static QsQualifiedName OriginalNameFromMonomorphized(QsQualifiedName mangled) =>
            new QsQualifiedName(
                mangled.Namespace,
                GUID.Replace(mangled.Name, string.Empty));
    }

    /// <summary>
    /// Upon transformation, assigns each defined variable a unique name, independent on the scope, and replaces all references to it accordingly.
    /// The original variable name can be recovered by using the static method StripUniqueName.
    /// This class is *not* threadsafe.
    /// </summary>
    public class UniqueVariableNames : MonoTransformation
    {
        private int variableNr = 0;
        private readonly Dictionary<string, string> uniqueNames = new Dictionary<string, string>();

        private bool TryGetUniqueName(string name, out string unique) =>
            this.uniqueNames.TryGetValue(name, out unique);

        private QsExpressionKind AdaptIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
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

        public UniqueVariableNames()
            : base()
        {
        }

        /* static methods for convenience */

        private static readonly NameDecorator Decorator = new NameDecorator("qsVar");

        public static string StripUniqueName(string uniqueName) => Decorator.Undecorate(uniqueName) ?? uniqueName;

        /* overrides */

        public override string OnLocalNameDeclaration(string name) =>
            this.GenerateUniqueName(name);

        public override string OnLocalName(string name) =>
            this.TryGetUniqueName(name, out var unique) ? unique : name;

        public override QsExpressionKind OnIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
            this.AdaptIdentifier(sym, tArgs);
    }

    /// <summary>
    /// A transformation that renames all references to each given qualified name.
    /// </summary>
    public class RenameReferences : MonoTransformation
    {
        private readonly IImmutableDictionary<QsQualifiedName, QsQualifiedName> names;

        /// <summary>
        /// Gets the renamed version of the qualified name if one exists; otherwise, returns the original name.
        /// </summary>
        /// <param name="name">The qualified name to rename.</param>
        /// <returns>
        /// The renamed version of the qualified name if one exists; otherwise, returns the original name.
        /// </returns>
        private QsQualifiedName GetNewName(QsQualifiedName name) => this.names.GetValueOrDefault(name) ?? name;

        /// <summary>
        /// Gets the renamed version of the user-defined type if one exists; otherwise, returns the original one.
        /// </summary>
        /// <returns>
        /// The renamed version of the user-defined type if one exists; otherwise, returns the original one.
        /// </returns>
        private UserDefinedType RenameUdt(UserDefinedType udt)
        {
            var newName = this.GetNewName(new QsQualifiedName(udt.Namespace, udt.Name));
            return udt.With(newName.Namespace, newName.Name);
        }

        /// <summary>
        /// Creates a new rename references transformation.
        /// </summary>
        /// <param name="names">The mapping from existing names to new names.</param>
        public RenameReferences(IImmutableDictionary<QsQualifiedName, QsQualifiedName> names)
        {
            this.names = names;
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
                qualifiedName: this.GetNewName(callable.QualifiedName),
                attributes: callable.Attributes.Select(this.OnAttribute).ToImmutableArray(),
                access: callable.Access,
                source: callable.Source,
                position: callable.Position,
                symbolRange: callable.SymbolRange,
                argumentTuple: this.OnArgumentTuple(callable.ArgumentTuple),
                signature: this.OnSignature(callable.Signature),
                documentation: this.OnDocumentation(callable.Documentation));

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
                    specialization.TypeArguments.Item.Select(this.OnType).ToImmutableArray())
                : QsNullable<ImmutableArray<ResolvedType>>.Null;
            return new SpecializationDeclarationHeader(
                kind: specialization.Kind,
                typeArguments: typeArguments,
                information: specialization.Information,
                parent: this.GetNewName(specialization.Parent),
                attributes: specialization.Attributes.Select(this.OnAttribute).ToImmutableArray(),
                source: specialization.Source,
                position: specialization.Position,
                headerRange: specialization.HeaderRange,
                documentation: this.OnDocumentation(specialization.Documentation));
        }

        /// <summary>
        /// Renames references in the type declaration header, including the name of the type itself.
        /// </summary>
        /// <param name="type">The type declaration header in which to rename references.</param>
        /// <returns>The type declaration header with renamed references.</returns>
        public TypeDeclarationHeader OnTypeDeclarationHeader(TypeDeclarationHeader type)
        {
            return new TypeDeclarationHeader(
                qualifiedName: this.GetNewName(type.QualifiedName),
                attributes: type.Attributes.Select(this.OnAttribute).ToImmutableArray(),
                access: type.Access,
                source: type.Source,
                position: type.Position,
                symbolRange: type.SymbolRange,
                type: this.OnType(type.Type),
                typeItems: this.OnTypeItems(type.TypeItems),
                documentation: this.OnDocumentation(type.Documentation));
        }

        public override QsTypeKind OnUserDefinedType(UserDefinedType udt) =>
                QsTypeKind.NewUserDefinedType(this.RenameUdt(udt));

        public override QsTypeKind OnTypeParameter(QsTypeParameter tp) =>
            QsTypeKind.NewTypeParameter(tp.With(this.GetNewName(tp.Origin)));

        public override QsExpressionKind OnIdentifier(Identifier id, QsNullable<ImmutableArray<ResolvedType>> typeArgs)
        {
            if (id is Identifier.GlobalCallable global)
            {
                id = Identifier.NewGlobalCallable(this.GetNewName(global.Item));
            }

            return base.OnIdentifier(id, typeArgs);
        }

        public override QsDeclarationAttribute OnAttribute(QsDeclarationAttribute attribute)
        {
            var argument = this.OnTypedExpression(attribute.Argument);
            var typeId = attribute.TypeId.IsValue
                ? QsNullable<UserDefinedType>.NewValue(this.RenameUdt(attribute.TypeId.Item))
                : attribute.TypeId;

            return new QsDeclarationAttribute(
                typeId, attribute.TypeIdRange, argument, attribute.Offset, attribute.Comments);
        }

        public override QsCallable OnCallableDeclaration(QsCallable callable) =>
            base.OnCallableDeclaration(callable.WithFullName(this.GetNewName));

        public override QsCustomType OnTypeDeclaration(QsCustomType type) =>
            base.OnTypeDeclaration(type.WithFullName(this.GetNewName));

        public override QsSpecialization OnSpecializationDeclaration(QsSpecialization spec) =>
            base.OnSpecializationDeclaration(spec.WithParent(this.GetNewName));
    }
}
