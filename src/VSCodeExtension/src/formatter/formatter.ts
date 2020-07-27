// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

import * as vscode from 'vscode';
import { FormatRule } from "./index";
import operationRules from './operation-rules';
import indentRules from './indent-rules';

const rules: FormatRule[] = [
    ...operationRules,
    ...indentRules,
];

const formatter = (code: string, rules: FormatRule[]): string =>
    rules.reduce((formattedCode, rule) => rule(formattedCode), code);

export const formatDocument = (document: vscode.TextDocument): vscode.TextEdit[] => {
    const lines: vscode.TextLine[] = Array.from(Array(document.lineCount), (_, lineNo) => document.lineAt(lineNo));
    const formattedLines: vscode.TextEdit[] = lines.map(line => {
        const formattedLine: string = formatter(line.text, rules);
        return (formattedLine !== line.text)
            ? vscode.TextEdit.replace(line.range, formattedLine)
            : null;
    }).filter((e): e is vscode.TextEdit => e != null);
    return formattedLines;
}
