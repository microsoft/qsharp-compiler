// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/**
 * Formats operation declarations.
 *
 * @param code incoming code
 *
 * @example `operation   Foo(q:Qubit,  n   :Int)   :  Bool` ->
 * `operation Foo (q : Qubit, n : Int) : Bool`
 */
export const argsRule = (code: string): string => {
    const declarationMatcher: RegExp = /(operation|function)\s*(\w+)\s*(\(.*\))\s*:\s*(\w+)/g;
    const firstArgMatcher: RegExp = /\(((\w*)\s*:\s*([a-zA-Z]+))/g
    const restArgMatcher: RegExp = /,\s*((\w*)\s*:\s*([a-zA-Z]+))/g;

    return code.replace(declarationMatcher, (match, keyword, opName, args, retType) => {
        args = args
            .replace(
                firstArgMatcher,
                (match: string, group: string, variable: string, type: string) =>
                    `(${variable} : ${type}`
            )
            .replace(
                restArgMatcher,
                (match: string, group: string, variable: string, type: string) =>
                    `, ${variable} : ${type}`
            );
        return `${keyword} ${opName} ${args} : ${retType}`;
    });
};

export default [argsRule];
