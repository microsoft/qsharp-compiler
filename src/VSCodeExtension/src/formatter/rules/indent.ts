// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { FormatRule } from "../formatter";

/**
 * Removes whitespace from left of `namespace`.
 *
 * @param code input string
 *
 * @example `   namespace ... {}` ->
 * `namespace ... {}`
 */
export const namespaceRule: FormatRule = (code: string) => {
    const operationMatcher: RegExp = /\s+(namespace)/g;

    return operationMatcher.test(code) ? code.trimLeft() : code;
};

export default [namespaceRule];
