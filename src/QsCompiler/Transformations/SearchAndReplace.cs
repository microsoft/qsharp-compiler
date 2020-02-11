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

namespace Microsoft.Quantum.QsCompiler.Transformations.SearchAndReplace
{
    using QsTypeKind = QsTypeKind<ResolvedType, UserDefinedType, QsTypeParameter, CallableInformation>;
    using QsExpressionKind = QsExpressionKind<TypedExpression, Identifier, ResolvedType>;
    using QsRangeInfo = QsNullable<Tuple<QsPositionInfo, QsPositionInfo>>;


    // routines for finding occurrences of symbols/identifiers

    public class __IdentifierReferences__
         : QsSyntaxTreeTransformation<__IdentifierReferences__.TransformationState>
    {

        public class TransformationState
        {

        }

        public __IdentifierReferences__() :
            base(new TransformationState())
        { }
    }

    /// <summary>
    /// Class that allows to walk the syntax tree and find all locations where a certain identifier occurs.
    /// If a set of source file names is given on initialization, the search is limited to callables and specializations in those files. 
    /// </summary>
    public class IdentifierReferences :
        SyntaxTreeTransformation<IdentifierLocation>
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

            public Location(NonNullable<string> source, Tuple<int,int> declOffset, QsLocation stmLoc, Tuple<QsPositionInfo, QsPositionInfo> range)
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

        public QsQualifiedName IdentifierName;
        public Tuple<NonNullable<string>, QsLocation> DeclarationLocation { get; private set; }
        public IEnumerable<Location> Locations => this._Scope.Locations;

        private readonly IImmutableSet<NonNullable<string>> RelevantSourseFiles;
        private bool IsRelevant(NonNullable<string> source) =>
            this.RelevantSourseFiles?.Contains(source) ?? true;

        public IdentifierReferences(QsQualifiedName idName, QsLocation defaultOffset, IImmutableSet<NonNullable<string>> limitToSourceFiles = null) :
            base(new IdentifierLocation(idName, defaultOffset))
        {
            this.IdentifierName = idName ?? throw new ArgumentNullException(nameof(idName));
            this.RelevantSourseFiles = limitToSourceFiles;
        }

        public override QsCustomType onType(QsCustomType t)
        {
            if (!this.IsRelevant(t.SourceFile) || t.Location.IsNull) return t;
            if (t.FullName.Equals(this.IdentifierName))
            { this.DeclarationLocation = new Tuple<NonNullable<string>, QsLocation>(t.SourceFile, t.Location.Item); }
            return base.onType(t);
        }

        public override QsCallable onCallableImplementation(QsCallable c)
        {
            if (!this.IsRelevant(c.SourceFile) || c.Location.IsNull) return c;
            if (c.FullName.Equals(this.IdentifierName))
            { this.DeclarationLocation = new Tuple<NonNullable<string>, QsLocation>(c.SourceFile, c.Location.Item); }
            return base.onCallableImplementation(c);
        }

        public override QsDeclarationAttribute onAttribute(QsDeclarationAttribute att)
        {
            var declRoot = this._Scope.DeclarationOffset;
            this._Scope.DeclarationOffset = att.Offset;
            if (att.TypeId.IsValue) this._Scope._Expression._Type.onUserDefinedType(att.TypeId.Item);
            this._Scope._Expression.Transform(att.Argument);
            this._Scope.DeclarationOffset = declRoot;
            return att;
        }

        public override QsSpecialization onSpecializationImplementation(QsSpecialization spec) =>
            this.IsRelevant(spec.SourceFile) ? base.onSpecializationImplementation(spec) : spec;

        public override QsNullable<QsLocation> onLocation(QsNullable<QsLocation> l)
        {
            this._Scope.DeclarationOffset = l.IsValue? l.Item.Offset : null;
            return l;
        }

        public override NonNullable<string> onSourceFile(NonNullable<string> f)
        {
            this._Scope.Source = f;
            return base.onSourceFile(f);
        }


        // static methods for convenience

        public static IEnumerable<Location> Find(QsQualifiedName idName, QsNamespace ns, QsLocation defaultOffset,
            out Tuple<NonNullable<string>, QsLocation> declarationLocation, IImmutableSet<NonNullable<string>> limitToSourceFiles = null)
        {
            var finder = new IdentifierReferences(idName, defaultOffset, limitToSourceFiles);
            finder.Transform(ns ?? throw new ArgumentNullException(nameof(ns)));
            declarationLocation = finder.DeclarationLocation;
            return finder.Locations;
        }
    }

    /// <summary>
    /// Class that allows to walk a scope and find all locations where a certain identifier occurs within an expression.
    /// If no source file is specified prior to transformation, its name is set to the empty string. 
    /// The DeclarationOffset needs to be set prior to transformation, and in particular after defining a source file.
    /// If no DefaultOffset is set on initialization then only the locations of occurrences within statements are logged. 
    /// </summary>
    public class IdentifierLocation :
        ScopeTransformation<Core.StatementKindTransformation, OnTypedExpression<IdentifierLocation.TypeLocation>>
    {
        public class TypeLocation :
            Core.ExpressionTypeTransformation
        {
            private readonly QsCodeOutput.ExpressionTypeToQs CodeOutput = new QsCodeOutput.ExpressionToQs()._Type;
            internal Action<Identifier, QsRangeInfo> OnIdentifier;

            public TypeLocation(Action<Identifier, QsRangeInfo> onIdentifier = null) :
                base(true) =>
                this.OnIdentifier = onIdentifier;

            public override QsTypeKind onUserDefinedType(UserDefinedType udt)
            {
                this.OnIdentifier?.Invoke(Identifier.NewGlobalCallable(new QsQualifiedName(udt.Namespace, udt.Name)), udt.Range);
                return QsTypeKind.NewUserDefinedType(udt);
            }

            public override QsTypeKind onTypeParameter(QsTypeParameter tp)
            {
                this.CodeOutput.onTypeParameter(tp);
                var tpName = NonNullable<string>.New(this.CodeOutput.Output ?? "");
                this.OnIdentifier?.Invoke(Identifier.NewLocalVariable(tpName), tp.Range);
                return QsTypeKind.NewTypeParameter(tp);
            }
        }


        private IdentifierLocation(Func<Identifier, bool> trackId, QsLocation defaultOffset) :
            base(null, new OnTypedExpression<TypeLocation>(null, _ => new TypeLocation(), recur: true))
        {
            this.TrackIdentifier = trackId ?? throw new ArgumentNullException(nameof(trackId));
            this.Locations = ImmutableList<IdentifierReferences.Location>.Empty;
            this.DefaultOffset = defaultOffset;
            this._Expression.OnExpression = this.OnExpression;
            this._Expression._Type.OnIdentifier = this.LogIdentifierLocation;
        }

        public IdentifierLocation(NonNullable<string> idName, QsLocation defaultOffset = null) :
            this(id => id is Identifier.LocalVariable varName && varName.Item.Value == idName.Value, defaultOffset)
        { }

        public IdentifierLocation(QsQualifiedName idName, QsLocation defaultOffset = null) :
            this(id => id is Identifier.GlobalCallable cName && cName.Item.Equals(idName), defaultOffset)
        { }

        private NonNullable<string> SourceFile;
        private Tuple<int, int> RootOffset;
        private QsLocation CurrentLocation;
        private readonly Func<Identifier, bool> TrackIdentifier;

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
            internal get => this.SourceFile;
            set
            {
                this.SourceFile = value;
                this.RootOffset = null;
                this.CurrentLocation = null;
            }
        }

        /// <summary>
        /// Whenever DeclarationOffset is set, the current statement offset is set to this default value.
        /// </summary>
        public readonly QsLocation DefaultOffset;
        public ImmutableList<IdentifierReferences.Location> Locations { get; private set; }

        public override QsNullable<QsLocation> onLocation(QsNullable<QsLocation> loc)
        {
            this.CurrentLocation = loc.IsValue ? loc.Item : null;
            return base.onLocation(loc);
        }

        private void LogIdentifierLocation(Identifier id, QsRangeInfo range)
        {
            if (this.TrackIdentifier(id) && this.CurrentLocation?.Offset != null && range.IsValue)
            {
                var idLoc = new IdentifierReferences.Location(this.SourceFile, this.RootOffset, this.CurrentLocation, range.Item);
                this.Locations = this.Locations.Add(idLoc);
            }
        }

        private void OnExpression(TypedExpression ex)
        {
            if (ex.Expression is QsExpressionKind<TypedExpression, Identifier, ResolvedType>.Identifier id)
            { this.LogIdentifierLocation(id.Item1, ex.Range); }
        }


        // static methods for convenience

        public static IEnumerable<IdentifierReferences.Location> Find(NonNullable<string> idName, QsScope scope,
            NonNullable<string> sourceFile, Tuple<int, int> rootLoc)
        {
            var finder = new IdentifierLocation(idName, null);
            finder.SourceFile = sourceFile;
            finder.RootOffset = rootLoc ?? throw new ArgumentNullException(nameof(rootLoc));
            finder.Transform(scope ?? throw new ArgumentNullException(nameof(scope)));
            return finder.Locations;
        }
    }


    // routines for finding all symbols/identifiers

    public class __AccumulateIdentifiers__
         : QsSyntaxTreeTransformation<__AccumulateIdentifiers__.TransformationState>
    {

        public class TransformationState
        {

        }

        public __AccumulateIdentifiers__() :
            base(new TransformationState())
        { }
    }

    /// <summary>
    /// Generates a look-up for all used local variables and their location in any of the transformed scopes, 
    /// as well as one for all local variables reassigned in any of the transformed scopes and their locations. 
    /// Note that the location information is relative to the root node, i.e. the start position of the containing specialization declaration. 
    /// </summary>
    public class AccumulateIdentifiers :
        ScopeTransformation<AccumulateIdentifiers.VariableReassignments, OnTypedExpression<Core.ExpressionTypeTransformation>>
    {
        private QsLocation StatementLocation;
        private Func<TypedExpression, TypedExpression> UpdatedExpression;

        private List<(NonNullable<string>, QsLocation)> UpdatedLocals;
        private List<(NonNullable<string>, QsLocation)> UsedLocals;

        public ILookup<NonNullable<string>, QsLocation> ReassignedVariables => 
            this.UpdatedLocals.ToLookup(var => var.Item1, var => var.Item2);

        public ILookup<NonNullable<string>, QsLocation> UsedLocalVariables =>
            this.UsedLocals.ToLookup(var => var.Item1, var => var.Item2);


        public AccumulateIdentifiers() :
            base(
                scope => new VariableReassignments(scope as AccumulateIdentifiers),
                new OnTypedExpression<Core.ExpressionTypeTransformation>(recur: true))
        {
            this.UpdatedLocals = new List<(NonNullable<string>, QsLocation)>();
            this.UsedLocals = new List<(NonNullable<string>, QsLocation)>();
            this._Expression.OnExpression = this.onLocal(this.UsedLocals);
            this.UpdatedExpression = new OnTypedExpression<Core.ExpressionTypeTransformation>(this.onLocal(this.UpdatedLocals), recur: true).Transform;
        }

        private Action<TypedExpression> onLocal(List<(NonNullable<string>, QsLocation)> accumulate) => (TypedExpression ex) =>
        {
            if (ex.Expression is QsExpressionKind.Identifier id &&
                id.Item1 is Identifier.LocalVariable var)
            {
                var range = ex.Range.IsValue ? ex.Range.Item : this.StatementLocation.Range;
                accumulate.Add((var.Item, new QsLocation(this.StatementLocation.Offset, range)));
            }
        };

        public override QsStatement onStatement(QsStatement stm)
        {
            this.StatementLocation = stm.Location.IsNull ? null : stm.Location.Item;
            this.StatementKind.Transform(stm.Statement);
            return stm;
        }


        // helper class

        public class VariableReassignments :
            StatementKindTransformation<AccumulateIdentifiers>
        {
            public VariableReassignments(AccumulateIdentifiers scope)
                : base(scope)
            { }

            public override QsStatementKind onValueUpdate(QsValueUpdate stm)
            {
                this._Scope.UpdatedExpression(stm.Lhs);
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

        public class StatementKindTransformation
            : Core.StatementKindTransformation<TransformationState>
        {
            public StatementKindTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override SymbolTuple onSymbolTuple(SymbolTuple syms) =>
                syms is SymbolTuple.VariableNameTuple tuple
                    ? SymbolTuple.NewVariableNameTuple(tuple.Item.Select(this.onSymbolTuple).ToImmutableArray())
                    : syms is SymbolTuple.VariableName varName
                    ? SymbolTuple.NewVariableName(this.Parent.InternalState.GenerateUniqueName(varName.Item))
                    : syms;
        }

        public class ExpressionKindTransformation
            : Core.ExpressionKindTransformation<TransformationState>
        {
            public ExpressionKindTransformation(QsSyntaxTreeTransformation<TransformationState> parent)
                : base(parent)
            { }

            public override QsExpressionKind onIdentifier(Identifier sym, QsNullable<ImmutableArray<ResolvedType>> tArgs) =>
                this.Parent.InternalState.AdaptIdentifier(sym, tArgs);
        }
    }


    // general purpose helpers

    /// <summary>
    /// Recursively applies the specified action OnExpression to each identifier expression upon transformation. 
    /// Does nothing upon transformation if no action is specified. 
    /// </summary>
    public class OnTypedExpression<T> :
        ExpressionTransformation<ExpressionKindTransformation<OnTypedExpression<T>>, T>
        where T : Core.ExpressionTypeTransformation
    {
        private readonly bool recur;

        public OnTypedExpression(Action<TypedExpression> onExpression = null, Func<OnTypedExpression<T>, T> typeTransformation = null, bool recur = false) :
            base(e => new ExpressionKindTransformation<OnTypedExpression<T>>(e as OnTypedExpression<T>),
                 e => typeTransformation?.Invoke(e as OnTypedExpression<T>))
        {
            this.OnExpression = onExpression;
            this.recur = recur;
        }

        public Action<TypedExpression> OnExpression;
        public override TypedExpression Transform(TypedExpression ex)
        {
            this.OnExpression?.Invoke(ex);
            return this.recur ? base.Transform(ex) : ex;
        }
    }
}
