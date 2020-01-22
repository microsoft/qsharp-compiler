// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module private Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxExtensions

open System.Collections.Immutable
open Microsoft.Quantum.QsCompiler.DataTypes
open Microsoft.Quantum.QsCompiler.SyntaxTokens 


type QsPositionInfo with 

    static member New (pos : FParsec.Position) = 
        {Line = int pos.Line; Column = int pos.Column}

    static member Range (pos1, pos2) = 
        Value (QsPositionInfo.New pos1, QsPositionInfo.New pos2)

    /// If the given right and left range both have contain a Value, returns the given kind as well as a Value with the compined range.
    /// Returns the given fallback and Null otherwise. 
    static member WithCombinedRange (lRange, rRange) kind fallback = 
        match (lRange, rRange) with
        | Value r1, Value r2 -> (kind, Value (fst r1, snd r2))
        | _ -> (fallback, Null)


type QsExpression with 

    static member New (value, range) = 
        {Expression = value; Range = QsPositionInfo.Range range}

    static member New (value, range) = 
        {Expression = value; Range = range}


type QsSpecializationGenerator with 

    static member New (value, range) = // we do not currently process/parse type specializations
        {Generator = value; TypeArguments = Null; Range = QsPositionInfo.Range range}

    static member New (value, range) = // we do not currently process/parse type specializations
        {Generator = value; TypeArguments = Null; Range = range} 


type QsSymbol with 

    static member New (value, range) = 
        {Symbol = value; Range = QsPositionInfo.Range range}

    static member New (value, range) = 
        {Symbol = value; Range = range}


type Characteristics with 
   
    static member New (kind, range) = 
        {Characteristics = kind; Range = QsPositionInfo.Range range}

    static member New (kind, range) = 
        {Characteristics = kind; Range = range}
    

type QsType with 

    static member New (value, range) = 
        {Type = value; Range = QsPositionInfo.Range range}

    static member New (value, range) = 
        {Type = value; Range = range}


type QsInitializer with 

    static member New (value, range) = 
        {Initializer = value; Range = QsPositionInfo.Range range}

    static member New (value, range) = 
        {Initializer = value; Range = range}


type QsCompilerDiagnostic with 

    /// Builds a diagnostic error for the given code and range, without any associated arguments.
    static member internal NewError code (startPos, endPos) = 
        QsCompilerDiagnostic.Error (code, []) (QsPositionInfo.New startPos, QsPositionInfo.New endPos)
        
    /// Builds a diagnostic warning for the given code and range, without any associated arguments.
    static member internal NewWarning code (startPos, endPos) = 
        QsCompilerDiagnostic.Warning (code, []) (QsPositionInfo.New startPos, QsPositionInfo.New endPos)
        
    /// Builds a diagnostic information for the given code and range, without any associated arguments.
    static member internal NewInfo code (startPos, endPos) = 
        QsCompilerDiagnostic.Info (code, []) (QsPositionInfo.New startPos, QsPositionInfo.New endPos)

    member this.WithRange range = 
        {this with Range = range}


type CallableSignature with

    static member internal New (((typeParams, (argType, returnType)), characteristics), modifiers) = {
            TypeParameters = typeParams
            Argument = argType 
            ReturnType = returnType 
            Characteristics = characteristics
            Modifiers = modifiers
        }

    static member internal Invalid = 
        let invalidType = (InvalidType, Null) |> QsType.New
        let invalidSymbol = (InvalidSymbol, Null) |> QsSymbol.New
        let invalidArg = QsTuple ([QsTupleItem (invalidSymbol, invalidType)].ToImmutableArray())
        CallableSignature.New (((ImmutableArray.Empty,
                                 (invalidArg, invalidType)),
                                (InvalidSetExpr, Null) |> Characteristics.New),
                               { Access = DefaultAccess })


type QsFragment with

    static member internal New (kind, (startPos, endPos), diagnostic, text) = {
            Kind = kind
            Range = (QsPositionInfo.New startPos, QsPositionInfo.New endPos)
            Diagnostics = diagnostic 
            Text = text
        }

    member this.WithRange range = {
            Kind = this.Kind
            Range = range
            Diagnostics = this.Diagnostics
            Text = this.Text
        } 

    