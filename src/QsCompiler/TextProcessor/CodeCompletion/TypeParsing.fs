// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.TypeParsing

open FParsec
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.ParsingPrimitives

#nowarn "40"


/// Parses the characteristics keyword followed by a characteristics expression.
let characteristicsAnnotation =
    let rec characteristicsExpression =
        parse {
            let infixOp = operator qsSetUnion.op "" <|> operator qsSetIntersection.op ""

            let expTerm =
                pcollect [ brackets (lTuple, rTuple) characteristicsExpression
                           expectedKeyword qsAdjSet
                           expectedKeyword qsCtlSet ]

            return! createExpressionParser pzero infixOp pzero expTerm
        }

    expectedKeyword qsCharacteristics ?>> characteristicsExpression

/// Parses types where the top-level type is not an array type.
let rec nonArrayType =
    let typeParameter = pchar '\'' >>. expectedId TypeParameter (term symbol)

    let operationType =
        let inOutType = qsType >>. opArrow >>. qsType

        brackets
            (lTuple, rTuple)
            (attempt (brackets (lTuple, rTuple) inOutType ?>> characteristicsAnnotation)
             <|> attempt (inOutType ?>> characteristicsAnnotation)
             <|> inOutType)

    let functionType = brackets (lTuple, rTuple) (qsType >>. fctArrow >>. qsType)

    let keywordType =
        [ qsBigInt
          qsBool
          qsDouble
          qsInt
          qsPauli
          qsQubit
          qsRange
          qsResult
          qsString
          qsUnit ]
        |> List.map expectedKeyword
        |> pcollect

    choice [ attempt typeParameter
             attempt operationType
             attempt functionType
             attempt (tuple1 qsType)
             keywordType <|>@ expectedQualifiedSymbol UserDefinedType ]

/// Parses a type.
and qsType =
    parse { return! nonArrayType .>> many (brackets (lArray, rArray) (emptySpace >>% [])) }
