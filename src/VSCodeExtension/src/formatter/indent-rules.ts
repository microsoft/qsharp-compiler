// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

import { FormatRule } from "./index";

/**
 * Removes whitespace from left of `namespace`.
 * 
 * @param code input string
 * 
 * @example `   namespace ... {}` ->
 * `namespace ... {}`
 */
const namespaceRule = (code: string): string => {
    const operationMatcher: RegExp = /\s+(namespace)/g;

    return (operationMatcher.test(code))
        ? code.trimLeft()
        : code;
};

const rules: FormatRule[] = [
    namespaceRule,
];

export default rules;
