// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/**
 * Formats operation declarations.
 *
 * @param code incoming code
 *
 * @example `operation   Foo(q:Qubit, n   :Int)   :  Bool` ->
 * `operation Foo (q : Qubit, n : Int) : Bool`
 */
export const argsRule = (code: string): string => {
    const operationMatcher: RegExp = /operation\s*(\w+)\s*\((.*)\)\s*:\s*(\w+)/g;
    const argumentMatcher: RegExp = /((\w*)\s*:\s*([a-zA-Z]+))/g;

    return code.replace(operationMatcher, (match, opName, args, retType) => {
        args = args.replace(
            argumentMatcher,
            (match: string, group1: string, variable: string, type: string) => {
                return `${variable} : ${type}`;
            }
        );
        return `operation ${opName} (${args}) : ${retType}`;
    });
};

export default [argsRule];
