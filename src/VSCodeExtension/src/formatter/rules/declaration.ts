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
    const declarationMatcher: RegExp = /(operation|function)\s*(\w+)\s*(\([^\)]*\))\s*:\s*(\w+)/g;

    return code.replace(declarationMatcher, (match, keyword, opName, args, retType) => {
        args = args
            // strip whitespace and newlines
            .replace(/\s+/g, '')
            .replace(/,/g, ', ')
            .replace(/:/g, ' : ');
        return `${keyword} ${opName} ${args} : ${retType}`;
    });
};

export default [argsRule];
