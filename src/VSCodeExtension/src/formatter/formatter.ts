// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export type FormatRule = (code: string) => string;

export const formatter = (code: string, rules: FormatRule[]): string =>
    rules.reduce((formattedCode, rule) => rule(formattedCode), code);

export const withCommentsIgnored = (rule: FormatRule): FormatRule => {
    return (code: string) => {
        const tokenizedCode = code.split(/(\/\/.*)/g);
        return tokenizedCode.reduce((previousCode, token) => {
            const tokenIsComment = token.startsWith("//")
            const formattedCode = tokenIsComment ? token : rule(token);
            return previousCode + formattedCode;
        }, "");
    }
};  