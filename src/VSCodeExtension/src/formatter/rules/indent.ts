// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import { FormatRule } from "../formatter";

/**
 * Removes whitespace from left of `namespace`.
 *
 * @param code input string
 *
 * @example `   namespace   Foo   {}` ->
 * `namespace Foo {}`
 */
export const namespaceRule: FormatRule = (code: string) => {
    const namespaceMatcher: RegExp = /^\s*namespace\s+(\w+(?:\.\w+)*)\s*({[\S\s]*}|{)\s*/g;

    return code.replace(namespaceMatcher, (match, namespace, body) => `namespace ${namespace} ${body}`);
};

export default [namespaceRule];
