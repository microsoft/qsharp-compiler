// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module Microsoft.Quantum.QsCompiler.SyntaxExtensions

open System.Collections.Generic
open System.Collections.ObjectModel
open System.Collections.Immutable
open System.Linq
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree


type QsQualifiedName with
    static member New(nsName, cName) = { Namespace = nsName; Name = cName }

type UserDefinedType with
    static member New(nsName, tName, range) =
        {
            Namespace = nsName
            Name = tName
            Range = range
        }

type QsTypeParameter with
    static member New(origin, tName, range) =
        {
            Origin = origin
            TypeName = tName
            Range = range
        }

type QsLocation with
    static member New(pos, range) = { Offset = pos; Range = range }

type InferredExpressionInformation with
    static member New(isMutable, quantumDep) =
        { IsMutable = isMutable; HasLocalQuantumDependency = quantumDep }

type LocalVariableDeclaration<'Name> with
    static member New isMutable ((pos, range), vName: 'Name, t, hasLocalQuantumDependency) =
        {
            VariableName = vName
            Type = t
            InferredInformation = InferredExpressionInformation.New(isMutable, hasLocalQuantumDependency)
            Position = pos
            Range = range
        }

type LocalDeclarations with
    static member New(variables: IEnumerable<_>) =
        { Variables = variables.ToImmutableArray() }

    static member Concat this other =
        LocalDeclarations.New(this.Variables.Concat other.Variables)

    member this.AsVariableLookup() =
        let localVars = this.Variables |> Seq.map (fun decl -> decl.VariableName, decl)
        new ReadOnlyDictionary<_, _>(localVars.ToDictionary(fst, snd))

type InferredCallableInformation with
    /// the default values are intrinsic: false, selfAdj: false
    static member New(?intrinsic, ?selfAdj) =
        { IsIntrinsic = defaultArg intrinsic false; IsSelfAdjoint = defaultArg selfAdj false }

type CallableInformation with
    static member New(characteristics, inferredInfo) =
        { Characteristics = characteristics; InferredInformation = inferredInfo }

type TypedExpression with
    /// Builds and returns a TypedExpression with the given properties.
    /// The UnresolvedType of the given expression is set to the given expression type, and
    /// the ResolvedType is set to the type constructed by resolving it using ResolveTypeParameters and the given look-up.
    static member New(expr, typeParamResolutions: ImmutableDictionary<_, _>, exType, exInfo, range) =
        {
            Expression = expr
            TypeArguments = TypedExpression.AsTypeArguments typeParamResolutions
            ResolvedType = ResolvedType.ResolveTypeParameters typeParamResolutions exType
            InferredInformation = exInfo
            Range = range
        }

type QsBinding<'T> with
    static member New kind (lhs, rhs) =
        {
            Kind = kind
            Lhs = lhs
            Rhs = rhs
        }

type QsValueUpdate with
    static member New(lhs, rhs) = { Lhs = lhs; Rhs = rhs }

type QsComments with
    static member New(before: IEnumerable<_>, after: IEnumerable<_>) =
        { OpeningComments = before.ToImmutableArray(); ClosingComments = after.ToImmutableArray() }

type QsScope with
    static member New(statements: IEnumerable<_>, parentSymbols) =
        { Statements = statements.ToImmutableArray(); KnownSymbols = parentSymbols }

type QsPositionedBlock with
    static member New comments location block =
        {
            Body = block
            Location = location
            Comments = comments
        }

type QsConditionalStatement with
    static member New(blocks: IEnumerable<_>, defaultBlock) =
        { ConditionalBlocks = blocks.ToImmutableArray(); Default = defaultBlock }

type QsForStatement with
    static member New(loopVar, iterable, body) =
        {
            LoopItem = loopVar
            IterationValues = iterable
            Body = body
        }

type QsWhileStatement with
    static member New(condition, body) = { Condition = condition; Body = body }

type QsRepeatStatement with
    static member New(repeatBlock, successCondition, fixupBlock) =
        {
            RepeatBlock = repeatBlock
            SuccessCondition = successCondition
            FixupBlock = fixupBlock
        }

type QsConjugation with
    static member New(outer, inner) =
        { OuterTransformation = outer; InnerTransformation = inner }

type QsQubitScope with
    static member New kind ((lhs, rhs), body) =
        {
            Kind = kind
            Binding = QsBinding<QsInitializer>.New QsBindingKind.ImmutableBinding (lhs, rhs)
            Body = body
        }

type QsStatement with
    static member New comments location (kind, symbolDecl) =
        {
            Statement = kind
            SymbolDeclarations = symbolDecl
            Location = location
            Comments = comments
        }

type ResolvedSignature with
    static member New((argType, returnType), info, typeParams: IEnumerable<_>) =
        {
            TypeParameters = typeParams.ToImmutableArray()
            ArgumentType = argType
            ReturnType = returnType
            Information = info
        }

type QsSpecialization with
    static member New kind
                      (source, location)
                      (parent, attributes, typeArgs, signature, implementation, documentation, comments)
                      =
        {
            Kind = kind
            Parent = parent
            Attributes = attributes
            Source = source
            Location = location
            TypeArguments = typeArgs
            Signature = signature
            Implementation = implementation
            Documentation = documentation
            Comments = comments
        }

    static member NewBody = QsSpecialization.New QsBody
    static member NewAdjoint = QsSpecialization.New QsAdjoint
    static member NewControlled = QsSpecialization.New QsControlled
    static member NewControlledAdjoint = QsSpecialization.New QsControlledAdjoint

type QsCallable with
    static member New kind
                      (source, location)
                      (name,
                       attributes,
                       visibility,
                       argTuple,
                       signature,
                       specializations: IEnumerable<_>,
                       documentation,
                       comments)
                      =
        {
            Kind = kind
            FullName = name
            Attributes = attributes
            Visibility = visibility
            Source = source
            Location = location
            Signature = signature
            Specializations = specializations.ToImmutableArray()
            ArgumentTuple = argTuple
            Documentation = documentation
            Comments = comments
        }

    static member NewFunction = QsCallable.New QsCallableKind.Function
    static member NewOperation = QsCallable.New QsCallableKind.Operation
    static member NewTypeConstructor = QsCallable.New QsCallableKind.TypeConstructor

type QsCustomType with
    static member New (source, location) (name, attributes, visibility, items, underlyingType, documentation, comments)
                      =
        {
            FullName = name
            Attributes = attributes
            Visibility = visibility
            Source = source
            Location = location
            Type = underlyingType
            TypeItems = items
            Documentation = documentation
            Comments = comments
        }

type QsDeclarationAttribute with
    static member New(typeId, arg, pos, comments) =
        {
            TypeId = typeId
            Argument = arg
            Offset = pos
            Comments = comments
        }

type QsNamespaceElement with
    static member NewOperation loc =
        QsCallable.NewOperation loc >> QsCallable

    static member NewFunction loc = QsCallable.NewFunction loc >> QsCallable
    static member NewType loc = QsCustomType.New loc >> QsCustomType

type QsNamespace with
    static member New(name, elements: IEnumerable<_>, documentation) =
        {
            Name = name
            Elements = elements.ToImmutableArray()
            Documentation = documentation
        }

type QsCompilation with
    static member New(namespaces, entryPoints) =
        { Namespaces = namespaces; EntryPoints = entryPoints }
