// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/**
 * Removes whitespace from left of `namespace`.
 * 
 * @param code input string
 * 
 * @example `   namespace ... {}` ->
 * `namespace ... {}`
 */
export const namespaceRule = (code: string): string => {
    const operationMatcher: RegExp = /\s+(namespace)/g;

    return (operationMatcher.test(code))
        ? code.trimLeft()
        : code;
};
