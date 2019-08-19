module Microsoft.Quantum.QsCompiler.CompilerOptimization.PureCircuitAPI

open System.Collections.Immutable
open System.Numerics
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens
open Microsoft.Quantum.QsCompiler.SyntaxTree

open ComputationExpressions
open Types
open Utils
open Printer


/// An identifier for a qubit or for an array of qubits
type QubitReference = Qubit of int | QubitArray of int

/// Any constant expression
type Const =
| UnitValue
| ValueTuple            of Const list
| IntLiteral            of int64
| BigIntLiteral         of BigInteger
| DoubleLiteral         of double
| BoolLiteral           of bool
| StringLiteral         of string
| ResultLiteral         of QsResult
| PauliLiteral          of QsPauli
| RangeLiteral          of Const * Const
| ValueArray            of Const list
| ArrayItem             of Const * Const
| QubitReference        of QubitReference

/// A call to a quantum gate, with the given functors and arguments
type GateCall = {
    gate:     QsQualifiedName
    adjoint:  bool
    controls: Const list
    arg:      Const
}

/// A pure, parameter-free quantum circuit
type Circuit = {
    numQubitRefs: int
    gates: GateCall list
}

/// Metadata used for constructing circuits
type private CircuitContext = {
    qubitReferences: string list
}


/// Converts a TypedExpression to a Const, mutating the given CircuitContext as needed.
/// Returns None if the given expression cannot be converted to a constant.
let rec private exprToConst (cc: CircuitContext ref) (expr: TypedExpression): Const option =
    match expr.Expression with
    | Expr.UnitValue -> UnitValue |> Some
    | Expr.ValueTuple x -> x |> Seq.map (exprToConst cc) |> List.ofSeq |> optionListToListOption |> Option.map ValueTuple
    | Expr.IntLiteral x -> IntLiteral x |> Some
    | Expr.BigIntLiteral x -> BigIntLiteral x |> Some
    | Expr.DoubleLiteral x -> DoubleLiteral x |> Some
    | Expr.BoolLiteral x -> BoolLiteral x |> Some
    | Expr.StringLiteral (x, _) -> StringLiteral x.Value |> Some
    | Expr.ResultLiteral x -> ResultLiteral x |> Some
    | Expr.PauliLiteral x -> PauliLiteral x |> Some
    | Expr.RangeLiteral (x, y) -> Option.map2 (fun x2 y2 -> RangeLiteral (x2, y2)) (exprToConst cc x) (exprToConst cc y)
    | Expr.ValueArray x -> x |> Seq.map (exprToConst cc) |> List.ofSeq |> optionListToListOption |> Option.map ValueArray
    | Expr.ArrayItem (x, y) -> Option.map2 (fun x2 y2 -> ArrayItem (x2, y2)) (exprToConst cc x) (exprToConst cc y)
    | Identifier (LocalVariable name, _) ->
        let constructor =
            match expr.ResolvedType.Resolution with
            | TypeKind.Qubit -> Some Qubit
            | TypeKind.ArrayType x when x.Resolution = TypeKind.Qubit -> Some QubitArray
            | _ -> None
        let index =
            match List.tryFindIndex name.Value.Equals (!cc).qubitReferences with
            | Some index -> index
            | None ->
                cc := {!cc with qubitReferences = (!cc).qubitReferences @ [name.Value]}
                (!cc).qubitReferences.Length - 1
        Option.map (fun c -> QubitReference (c index)) constructor
    | _ -> None


/// Converts a TypedExpression to a GateCall, mutating the given CircuitContext as needed.
/// Returns None if the given expression cannot be converted to a gate call.
let private exprToGateCall (cc: CircuitContext ref) (expr: TypedExpression): GateCall option =
    let rec helper method arg: GateCall option = maybe {
        match method.Expression with
        | AdjointApplication x ->
            let! result = helper x arg
            return { result with adjoint = not result.adjoint }
        | ControlledApplication x ->
            match arg.Expression with
            | Expr.ValueTuple vt ->
                do! check (vt.Length = 2)
                let! c = exprToConst cc vt.[0]
                let! result = helper x vt.[1]
                return { result with controls = c :: result.controls }
            | _ -> return! None
        | Identifier (GlobalCallable name, _) ->
            let! argVal = exprToConst cc arg
            return { gate = name; adjoint = false; controls = []; arg = argVal }
        | _ ->
            return! None
    }
    match expr.Expression with
    | CallLikeExpression (method, arg) ->
        helper method arg
    | _ ->
        None


/// Converts a list of TypedExpressions to a Circuit, CircuitContext pair.
/// Returns None if the given expression list cannot be converted to a pure circuit.
let private exprListToCircuit (exprList: TypedExpression list): (Circuit * CircuitContext) option =
    let s = exprList |> List.map (fun x -> printExpr x.Expression)
    let cc = ref { qubitReferences = [] }
    let gates = exprList |> List.map (exprToGateCall cc) |> optionListToListOption
    gates |> Option.map (fun x -> { numQubitRefs = (!cc).qubitReferences.Length; gates = x }, !cc)


/// Returns the Q# type corresponding to the given Const
let rec private constToType (c: Const): ResolvedType =
    let constToTypeKind = function
    | UnitValue -> UnitType
    | ValueTuple cl -> cl |> Seq.map constToType |> ImmutableArray.CreateRange |> TupleType
    | IntLiteral _ -> Int
    | BigIntLiteral _ -> BigInt
    | DoubleLiteral _ -> Double
    | BoolLiteral _ -> Bool
    | StringLiteral _ -> String
    | ResultLiteral _ -> Result
    | PauliLiteral _ -> Pauli
    | RangeLiteral _ -> Range
    | ValueArray cl -> ArrayType (constToType cl.[0])
    | ArrayItem (c1, c2) ->
        match (constToType c1).Resolution, (constToType c2).Resolution with
        | t, Range | t, ArrayType _ -> t
        | ArrayType t, Int -> t.Resolution
        | _ -> failwithf "Invalid ArrayItem node"
    | QubitReference (Qubit _) -> TypeKind.Qubit
    | QubitReference (QubitArray _) -> ArrayType (ResolvedType.New TypeKind.Qubit)
    constToTypeKind c |> ResolvedType.New


/// Returns the Q# expression corresponding to the given Const
let rec private constToExpr (cc: CircuitContext) (c: Const): TypedExpression =
    let constToExprKind = function
    | UnitValue -> Expr.UnitValue
    | ValueTuple cl -> cl |> Seq.map (constToExpr cc) |> ImmutableArray.CreateRange |> Expr.ValueTuple
    | IntLiteral x -> Expr.IntLiteral x
    | BigIntLiteral x -> Expr.BigIntLiteral x
    | DoubleLiteral x -> Expr.DoubleLiteral x
    | BoolLiteral x -> Expr.BoolLiteral x
    | StringLiteral x -> Expr.StringLiteral (NonNullable<_>.New x, ImmutableArray.Empty)
    | ResultLiteral x -> Expr.ResultLiteral x
    | PauliLiteral x -> Expr.PauliLiteral x
    | RangeLiteral (x, y) -> Expr.RangeLiteral (constToExpr cc x, constToExpr cc y)
    | ValueArray cl -> cl |> Seq.map (constToExpr cc) |> ImmutableArray.CreateRange |> Expr.ValueArray
    | ArrayItem (x, y) -> Expr.ArrayItem (constToExpr cc x, constToExpr cc y)
    | QubitReference (Qubit i) -> Identifier (LocalVariable (NonNullable<_>.New cc.qubitReferences.[i]), Null)
    | QubitReference (QubitArray i) -> Identifier (LocalVariable (NonNullable<_>.New cc.qubitReferences.[i]), Null)
    wrapExpr (constToType c).Resolution (constToExprKind c)


/// Returns the Q# expression correponding to the given gate call
let private gateCallToExpr (cc: CircuitContext) (gc: GateCall): TypedExpression =
    let mutable method = wrapExpr UnitType (Identifier (GlobalCallable gc.gate, Null))
    let mutable arg = constToExpr cc gc.arg

    if gc.adjoint then
        method <- wrapExpr UnitType (AdjointApplication method)
    for control in gc.controls do
        method <- wrapExpr UnitType (ControlledApplication method)
        arg <- constToExpr cc (ValueTuple [control; exprToConst (ref cc) arg |> Option.get])

    wrapExpr UnitType (CallLikeExpression (method, arg))


/// Returns the list of Q# expressions corresponding to the given circuit
let private circuitToExprList (cc: CircuitContext) (circuit: Circuit): TypedExpression list =
    List.map (gateCallToExpr cc) circuit.gates


/// Given a pure circuit, performs basic optimizations and returns the new circuit
let private optimizeCircuit (circuit: Circuit): Circuit =
    let mutable circuit = circuit
    let mutable i = 0
    while i < circuit.gates.Length - 1 do
        if circuit.gates.[i] = { circuit.gates.[i+1] with adjoint = not circuit.gates.[i+1].adjoint } then
            circuit <- { circuit with gates = removeIndices [i; i+1] circuit.gates}
        elif circuit.gates.[i] = circuit.gates.[i+1] && List.contains circuit.gates.[i].gate.Name.Value ["X"; "Z"; "H"; "CNOT"] then
            circuit <- { circuit with gates = removeIndices [i; i+1] circuit.gates}
        else
            i <- i + 1
    circuit


/// Given a list of Q# expressions, tries to convert it to a pure circuit.
/// If this succeeds, optimizes the circuit and returns the new list of Q# expressions.
/// Otherwise, returns the given list.
let optimizeExprList (exprList: TypedExpression list): TypedExpression list =
    let s = List.map (fun x -> printExpr x.Expression) exprList
    if exprList.Length >= 5 then
        printfn "%O" s
    match exprListToCircuit exprList with
    | Some (circuit, cc) ->
        circuitToExprList cc (optimizeCircuit circuit)
    | None -> exprList
