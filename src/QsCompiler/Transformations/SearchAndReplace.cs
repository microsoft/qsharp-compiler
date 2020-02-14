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
using Microsoft.Quantum.QsCompiler.Transformations.Core;
using Microsoft.Quantum.QsCompiler.Transformations.QsCodeOutput;


namespace Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace
{
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using QsRangeInfo = QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>;


    // routines for finding occurrences of symbols/identifiers

    /// <summary>
    /// Class that allows to walk the syntax tree and find all locations where a certain identifier occurs.
    /// If a set of source file names is given on initialization, the search is limited to callables and specializations in those files.
    /// </summary>
    public class IdentifierReferences
         : QsSyntaxTreeTransformation<IdentifierReferences.TransformationState>
    {
        public class Location : IEquatable<Location>
        {
            public readonly NonNullable<string> SourceFile;
            /// <summary>
            /// contains the offset of the root node relative to which the statement location is given
            /// </summary>
            public readonly Tuple<int, int> DeclarationOffset;
            /// <summary>
            /// contains the location of the statement containing the symbol relative to the root node
            /// </summary>
            public readonly QsLocation RelativeStatementLocation;
            /// <summary>
            /// contains the range of the symbol relative to the statement position
            /// </summary>
            public readonly Tuple<QsPositionInfo, QsPositionInfo> SymbolRange;

            public Location(NonNullable<string> source, Tuple<int, int> declOffset, QsLocation stmLoc, Tuple<QsPositionInfo, QsPositionInfo> range)
            {
                this.SourceFile = source;
                this.DeclarationOffset = declOffset ?? throw new ArgumentNullException(nameof(declOffset));
                this.RelativeStatementLocation = stmLoc ?? throw new ArgumentNullException(nameof(stmLoc));
                this.SymbolRange = range ?? throw new ArgumentNullException(nameof(range));
            }

            public bool Equals(Location other) =>
                this.SourceFile.Value == other?.SourceFile.Value
                && this.DeclarationOffset == other?.DeclarationOffset
                && this.RelativeStatementLocation == other?.RelativeStatementLocation
                && this.SymbolRange.Item1.Equals(other?.SymbolRange?.Item1)
                && this.SymbolRange.Item2.Equals(other?.SymbolRange?.Item2);

            public override bool Equals(object obj) =>
                this.Equals(obj as Location);

            public override int GetHashCode()
            {
                var (hash, multiplier) = (0x51ed270b, -1521134295);
                if (this.DeclarationOffset != null) hash = (hash * multiplier) + this.DeclarationOffset.GetHashCode();
                if (this.RelativeStatementLocation.Offset != null) hash = (hash * multiplier) + this.RelativeStatementLocation.Offset.GetHashCode();
                if (this.RelativeStatementLocation.Range != null) hash = (hash * multiplier) + this.RelativeStatementLocation.Range.GetHashCode();
                if (this.SymbolRange != null) hash = (hash * multiplier) + this.SymbolRange.GetHashCode();
                return this.SourceFile.Value == null ? hash : (hash * multiplier) + this.SourceFile.Value.GetHashCode();
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
            public Tuple<NonNullable<string>, QsLocation> DeclarationLocation { get; internal set; }
            public ImmutableList<Location> Locations { get; private set; }

            /// <summary>
            /// Whenever DeclarationOffset is set, the current statement offset is set to this default value.
            /// </summary>
            private readonly QsLocation DefaultOffset = null;
            private readonly IImmutableSet<NonNullable<string>> RelevantSourseFiles = null;

            internal bool IsRelevant(NonNullable<string> source) =>
                this.RelevantSourseFiles?.Contains(source) ?? true;


            internal TransformationState(Func<Identifier, bool> trackId,
                QsLocation defaultOffset = null, IImmutableSet<NonNullable<string>> limitToSourceFiles = null)
            {
                this.TrackIdentifier = trackId ?? throw new ArgumentNullException(nameof(trackId));
                this.RelevantSourseFiles = limitToSourceFiles;
                this.Locations = ImmutableList<Location>.Empty;
                this.DefaultOffset = defaultOffset;
            }

            private NonNullable<string> CurrentSourceFile = NonNullable<string>.New("");
            private Tuple<int, int> RootOffset = null;
            internal QsLocation CurrentLocation = null;
            internal readonly Func<Identifier, bool> TrackIdentifier;

            public Tuple<int, int> DeclarationOffset
            {
                internal get => this.RootOffset;
                set
                {
                    this.RootOffset = value ?? throw new ArgumentNullException(nameof(value), "declaration offset cannot be null");
                    this.CurrentLocation = this.DefaultOffset;
                }
            }

            public NonNullable<string> Source
            {
                internal get => this.CurrentSourceFile;
                set
                {
                    this.CurrentSourceFile = value;
                    this.RootOffset = null;
                    this.CurrentLocation = null;
                }
            }

            internal void LogIdentifierLocation(Identifier id, QsRangeInfo range)
            {
                if (this.TrackIdentifier(id) && this.CurrentLocation?.Offset != null && range.IsValue)
                {
                    var idLoc = new Location(this.Source, this.RootOffset, this.CurrentLocation, range.Item);
                    this.Locations = this.Locations.Add(idLoc);
                }
            }

            internal void LogIdentifierLocation(TypedExpression ex)
            {
                if (ex.Expression is QsExpressionKind.Identifier id)
                { this.LogIdentifierLocation(id.Item1, ex.Range); }
            }
        }


        public IdentifierReferences(NonNullable<string> idName, QsLocation defaultOffset, IImmutableSet<NonNullable<string>> limitToSourceFiles = null) :
            base(new TransformationState(id => id is Identifier.LocalVariable varName && varName.Item.Value == idName.Value, defaultOffset, limitToSourceFiles))
        { }

        public IdentifierReferences(QsQualifiedName idName, QsLocation defaultOffset, IImmutableSet<NonNullable<string>> limitToSourceFiles = null) :
            base(new TransformationState(id => id is Identifier.GlobalCallable cName && cName.Item.Equals(idName), defaultOffset, limitToSourceFiles))
        {
            if (idName == null) throw new ArgumentNullException(nameof(idName));
        }

        public override TypeTransformation<TransformationState> NewTypeTransformation() =>
            new ExpressionTypeTransformation(this);

        public override Core.ExpressionTransformation<TransformationState> NewExpressionTransformation() =>
            new TypedExpressionWalker<TransformationState>(this.InternalState.LogIdentifierLocation, this);

        public override StatementTransformation<TransformationState> NewStatementTransformation() =>
            new StatementTransformation(this);

        public override NamespaceTransformation<TransformationState> NewNamespaceTransformation() =>
            new NamespaceTransformation(this);


        // static methods for convenience

        public static IEnumerable<Location> Find(NonNullable<string> idName, QsScope scope,
            NonNullable<string> sourceFile, Tuple<int, int> rootLoc)
        {
            var finder = new IdentifierReferences(idName, null, ImmutableHashSet.Create(sourceFile));
            finder.InternalState.Source = sourceFile;
            finder.InternalState.DeclarationOffset = rootLoc; // will throw if null
            finder.Statements.Transform(scope ?? throw new ArgumentNullException(nameof(scope)));
            return finder.InternalState.Locations;
        }

        public static IEnumerable<Location> Find(QsQualifiedName idName, QsNamespace ns, QsLocation defaultOffset,
            out Tuple<NonNullable<string>, QsLocation> declarationLocation, IImmutableSet<NonNullable<string>> limitToSourceFiles = null)
        {
            var finder = new IdentifierReferences(idName, defaultOffset, limitToSourceFiles);
            finder.Namespaces.Transform(ns ?? throw new ArgumentNullException(nameof(ns)));
            declarationLocation = finder.InternalState.DeclarationLocation;
            return finder.InternalState.Locations;
        }


        // helper classes

        private class ExpressionTypeTransformation :
            TypeTransformation<TransformationState>
        {
            public ExpressionTypeTransformation(QsSyntaxTreeTransformation<TransformationState> parent) :
                base(parent)
            { }

            public override QsTypeKind onUserDefinedType(UserDefinedType udt)
            {
                var id = Identifier.NewGlobalCallable(new QsQualifiedName(udt.Namespace, udt.Name));
                this.Transformation.InternalState.LogIdentifierLocation(id, udt.Range);
                return QsTypeKind.NewUserDefinedType(udt);
            }

            public override QsTypeKind onTypeParameter(QsTypeParameter tp)
            {
                var resT = ResolvedType.New(QsTypeKind.NewTypeParameter(tp));
                var id = Identifier.NewLocalVariable(NonNullable<string>.New(SyntaxTreeToQs.Default.ToCode(resT) ?? ""));
                this.Transformation.InternalState.LogIdentifierLocation(id, tp.Range);
                return resT.Resolution;
            }
        }

        private class StatementTransformation :
            StatementTransformation<TransformationState>
        {
            public StatementTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override QsNullable<QsLocation> onLocation(QsNullable<QsLocation> loc)
            {
                this.Transformation.InternalState.CurrentLocation = loc.IsValue ? loc.Item : null;
                return loc;
            }
        }

        private class NamespaceTransformation :
            NamespaceTransformation<TransformationState>
        {

            public NamespaceTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override QsCustomType onType(QsCustomType t)
            {
                if (!this.Transformation.InternalState.IsRelevant(t.SourceFile) || t.Location.IsNull) return t;
                if (this.Transformation.InternalState.TrackIdentifier(Identifier.NewGlobalCallable(t.FullName)))
                { this.Transformation.InternalState.DeclarationLocation = new Tuple<NonNullable<string>, QsLocation>(t.SourceFile, t.Location.Item); }
                return base.onType(t);
            }

            public override QsCallable onCallableImplementation(QsCallable c)
            {
                if (!this.Transformation.InternalState.IsRelevant(c.SourceFile) || c.Location.IsNull) return c;
                if (this.Transformation.InternalState.TrackIdentifier(Identifier.NewGlobalCallable(c.FullName)))
                { this.Transformation.InternalState.DeclarationLocation = new Tuple<NonNullable<string>, QsLocation>(c.SourceFile, c.Location.Item); }
                return base.onCallableImplementation(c);
            }

            public override QsDeclarationAttribute onAttribute(QsDeclarationAttribute att)
            {
                var declRoot = this.Transformation.InternalState.DeclarationOffset;
                this.Transformation.InternalState.DeclarationOffset = att.Offset;
                if (att.TypeId.IsValue) this.Transformation.Types.onUserDefinedType(att.TypeId.Item);
                this.Transformation.Expressions.Transform(att.Argument);
                this.Transformation.InternalState.DeclarationOffset = declRoot;
                return att;
            }

            public override QsSpecialization onSpecializationImplementation(QsSpecialization spec) =>
                this.Transformation.InternalState.IsRelevant(spec.SourceFile) ? base.onSpecializationImplementation(spec) : spec;

            public override QsNullable<QsLocation> onLocation(QsNullable<QsLocation> loc)
            {
                this.Transformation.InternalState.DeclarationOffset = loc.IsValue ? loc.Item.Offset : null;
                return loc;
            }

            public override NonNullable<string> onSourceFile(NonNullable<string> source)
            {
                this.Transformation.InternalState.Source = source;
                return source;
            }
        }
    }


    // routines for finding all symbols/identifiers

    /// <summary>
    /// Generates a look-up for all used local variables and their location in any of the transformed scopes,
    /// as well as one for all local variables reassigned in any of the transformed scopes and their locations.
    /// Note that the location information is relative to the root node, i.e. the start position of the containing specialization declaration.
    /// </summary>
    public class AccumulateIdentifiers
         : QsSyntaxTreeTransformation<AccumulateIdentifiers.TransformationState>
    {
        public class TransformationState
        {
            internal QsLocation StatementLocation = null;
            internal Func<TypedExpression, TypedExpression> UpdatedExpression;

            private readonly List<(NonNullable<string>, QsLocation)> UpdatedLocals = new List<(NonNullable<string>, QsLocation)>();
            private readonly List<(NonNullable<string>, QsLocation)> UsedLocals = new List<(NonNullable<string>, QsLocation)>();

            internal TransformationState() =>
                this.UpdatedExpression = new TypedExpressionWalker<TransformationState>(this.UpdatedLocal, this).Transform;

            public ILookup<NonNullable<string>, QsLocation> ReassignedVariables =>
                this.UpdatedLocals.ToLookup(var => var.Item1, var => var.Item2);

            public ILookup<NonNullable<string>, QsLocation> UsedLocalVariables =>
                this.UsedLocals.ToLookup(var => var.Item1, var => var.Item2);


            private Action<TypedExpression> Add(List<(NonNullable<string>, QsLocation)> accumulate) => (TypedExpression ex) =>
            {
                if (ex.Expression is QsExpressionKind.Identifier id &&
                    id.Item1 is Identifier.LocalVariable var)
                {
                    var range = ex.Range.IsValue ? ex.Range.Item : this.StatementLocation.Range;
                    accumulate.Add((var.Item, new QsLocation(this.StatementLocation.Offset, range)));
                }
            };

            internal Action<TypedExpression> UsedLocal => Add(this.UsedLocals);
            internal Action<TypedExpression> UpdatedLocal => Add(this.UpdatedLocals);
        }


        public AccumulateIdentifiers() :
            base(new TransformationState())
        { }

        public override Core.ExpressionTransformation<TransformationState> NewExpressionTransformation() =>
            new TypedExpressionWalker<TransformationState>(this.InternalState.UsedLocal, this);

        public override Core.StatementKindTransformation<TransformationState> NewStatementKindTransformation() =>
            new StatementKindTransformation(this);

        public override StatementTransformation<TransformationState> NewStatementTransformation() =>
            new StatementTransformation(this);


        // helper classes

        private class StatementTransformation :
            StatementTransformation<TransformationState>
        {
            public StatementTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override QsStatement onStatement(QsStatement stm)
            {
                this.Transformation.InternalState.StatementLocation = stm.Location.IsNull ? null : stm.Location.Item;
                this.StatementKind.Transform(stm.Statement);
                return stm;
            }
        }

        private class StatementKindTransformation :
            Core.StatementKindTransformation<TransformationState>
        {
            public StatementKindTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override QsStatementKind onValueUpdate(QsValueUpdate stm)
            {
                this.Transformation.InternalState.UpdatedExpression(stm.Lhs);
                this.ExpressionTransformation(stm.Rhs);
                return QsStatementKind.NewQsValueUpdate(stm);
            }
        }
    }


    // routines for replacing symbols/identifiers

    /// <summary>
    /// Upon transformation, assigns each defined variable a unique name, independent on the scope, and replaces all references to it accordingly.
    /// The original variable name can be recovered by using the static method StripUniqueName.
    /// This class is *not* threadsafe.
    /// </summary>
    public class UniqueVariableNames
         : QsSyntaxTreeTransformation<UniqueVariableNames.TransformationState>
    {
        public class TransformationState
        {
            private int VariableNr = 0;
            private Dictionary<NonNullable<string>, NonNullable<string>> UniqueNames =
                new Dictionary<NonNullable<string>, NonNullable<string>>();

            internal QsExpressionKind AdaptIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
                sym is Identifier.LocalVariable varName && this.UniqueNames.TryGetValue(varName.Item, out var unique)
                    ? QsExpressionKind.NewIdentifier(Identifier.NewLocalVariable(unique), tArgs)
                    : QsExpressionKind.NewIdentifier(sym, tArgs);

            /// <summary>
            /// Will overwrite the dictionary entry mapping a variable name to the corresponding unique name if the key already exists.
            /// </summary>
            internal NonNullable<string> GenerateUniqueName(NonNullable<string> varName)
            {
                var unique = NonNullable<string>.New($"__{Prefix}{this.VariableNr++}__{varName.Value}__");
                this.UniqueNames[varName] = unique;
                return unique;
            }
        }


        private const string Prefix = "qsVar";
        private const string OrigVarName = "origVarName";
        private static readonly Regex WrappedVarName = new Regex($"^__{Prefix}[0-9]*__(?<{OrigVarName}>.*)__$");

        public NonNullable<string> StripUniqueName(NonNullable<string> uniqueName)
        {
            var matched = WrappedVarName.Match(uniqueName.Value).Groups[OrigVarName];
            return matched.Success ? NonNullable<string>.New(matched.Value) : uniqueName;
        }


        public UniqueVariableNames() :
            base(new TransformationState())
        { }

        public override Core.ExpressionKindTransformation<TransformationState> NewExpressionKindTransformation() =>
            new ExpressionKindTransformation(this);

        public override Core.StatementKindTransformation<TransformationState> NewStatementKindTransformation() =>
            new StatementKindTransformation(this);


        // helper classes

        private class StatementKindTransformation
            : Core.StatementKindTransformation<TransformationState>
        {
            public StatementKindTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override SymbolTuple onSymbolTuple(SymbolTuple syms) =>
                syms is SymbolTuple.VariableNameTuple tuple
                    ? SymbolTuple.NewVariableNameTuple(tuple.Item.Select(this.onSymbolTuple).ToImmutableArray())
                    : syms is SymbolTuple.VariableName varName
                    ? SymbolTuple.NewVariableName(this.Transformation.InternalState.GenerateUniqueName(varName.Item))
                    : syms;
        }

        private class ExpressionKindTransformation
            : Core.ExpressionKindTransformation<TransformationState>
        {
            public ExpressionKindTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override QsExpressionKind onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
                this.Transformation.InternalState.AdaptIdentifier(sym, tArgs);
        }
    }


    // general purpose helpers

    /// <summary>
    /// Upon transformation, applies the specified action to each expression and subexpression.
    /// The action to apply is specified upon construction, and will be applied before recurring into subexpressions.
    /// </summary>
    public class TypedExpressionWalker<T> :
        Core.ExpressionTransformation<T>
    {
        public TypedExpressionWalker(Action<TypedExpression> onExpression, QsSyntaxTreeTransformation<T> parent)
            : base(parent) =>
            this.OnExpression = onExpression ?? throw new ArgumentNullException(nameof(onExpression));

        public TypedExpressionWalker(Action<TypedExpression> onExpression, T internalState = default)
            : base(internalState) =>
            this.OnExpression = onExpression ?? throw new ArgumentNullException(nameof(onExpression));

        public readonly Action<TypedExpression> OnExpression;
        public override TypedExpression Transform(TypedExpression ex)
        {
            this.OnExpression(ex);
            return base.Transform(ex);
        }
    }
}
