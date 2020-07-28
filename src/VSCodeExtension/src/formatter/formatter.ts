// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export type FormatRule = (code: string) => string;

export const formatter = (code: string, rules: FormatRule[]): string =>
    rules.reduce((formattedCode, rule) => rule(formattedCode), code);
