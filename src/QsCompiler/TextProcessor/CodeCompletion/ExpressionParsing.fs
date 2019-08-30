module internal Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.ExpressionParsing

open System
open FParsec
open Microsoft.Quantum.QsCompiler.TextProcessing.ExpressionParsing
open Microsoft.Quantum.QsCompiler.TextProcessing.Keywords
open Microsoft.Quantum.QsCompiler.TextProcessing.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.SyntaxBuilder
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.ParsingPrimitives
open Microsoft.Quantum.QsCompiler.TextProcessing.CodeCompletion.TypeParsing

#nowarn "40"


/// Parses an expression.
let rec expression = parse {
    let prefixOp = expectedKeyword notOperator <|> operator qsNEGop.op "" <|> operator qsBNOTop.op ""

    let infixOp = operatorLike "" <|> pcollect [expectedKeyword andOperator; expectedKeyword orOperator]

    let postfixOp =
        let copyAndUpdate =
            operator qsCopyAndUpdateOp.op "" >>.
            (expression <|>@ expectedId NamedItem (term symbol)) ?>>
            expected (operator qsCopyAndUpdateOp.cont "") ?>>
            expression
        let typeParamListOrLessThan =
            // This is a parsing hack for the < operator, which can be either less-than or the start of a type parameter
            // list.
            operator qsLTop.op "=-" ?>> ((sepByLast qsType comma ?>> expected (bracket rAngle)) <|>@ expression)
        choice [
            operator qsUnwrapModifier.op ""
            pstring qsOpenRangeOp.op .>> emptySpace >>. optional eot >>% []
            operator qsNamedItemCombinator.op "" ?>> expectedId NamedItem (term symbol)
            copyAndUpdate
            tuple expression
            array expression
            typeParamListOrLessThan
        ]
    
    let expTerm =
        let newArray =
            expectedKeyword arrayDecl ?>>
            nonArrayType ?>>
            manyR (brackets (lArray, rArray) (emptySpace >>% [])) (preturn ()) @>>
            array expression
        let keywordLiteral =
            [
                qsPauliX
                qsPauliY
                qsPauliZ
                qsPauliI
                qsZero
                qsOne
                qsTrue
                qsFalse
                missingExpr
            ] |> List.map expectedKeyword |> pcollect
        let stringLiteral =
            let unescaped p = previousCharSatisfies ((<>) '\\') >>. p
            let quote = pstring "\""
            let interpolated =
                let text =
                    manyChars (notFollowedBy (unescaped quote <|> unescaped lCurly) >>. anyChar) >>.
                    optional eot >>. preturn []
                let code = brackets (lCurly, rCurly) expression
                pchar '$' >>. expected quote ?>> text ?>> manyLast (code ?>> text) ?>> expected quote
            let uninterpolated =
                let text = manyChars (notFollowedBy (unescaped quote) >>. anyChar) >>. optional eot >>. preturn []
                quote >>. text ?>> expected quote
            interpolated <|> uninterpolated
        let functor = expectedKeyword qsAdjointFunctor <|>@ expectedKeyword qsControlledFunctor
        pcollect [
            operator qsOpenRangeOp.op "" >>. opt expression |>> Option.defaultValue []
            newArray
            tuple1 expression
            array expression
            keywordLiteral
            numericLiteral >>. (previousCharSatisfiesNot Char.IsWhiteSpace >>. optional eot >>% [] <|> preturn [])
            stringLiteral
            manyR functor symbol @>> expectedQualifiedSymbol Callable
            expectedId Variable (term symbol) .>> nextCharSatisfiesNot ((=) '.')
        ]

    return! createExpressionParser prefixOp infixOp postfixOp expTerm
}
