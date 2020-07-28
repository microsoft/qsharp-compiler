// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as vscode from "vscode";
import { FormatRule, formatter } from "./formatter";
import indentRules from "./rules/indent";
import operationRules from "./rules/operation";
import controlStructureRules from "./rules/control-structure";

const rules: FormatRule[] = [
    ...indentRules,
    ...operationRules,
    ...controlStructureRules,
];

export const formatDocument = (
    document: vscode.TextDocument
): vscode.TextEdit[] => {
    const lines: vscode.TextLine[] = Array.from(
        Array(document.lineCount),
        (_, lineNo) => document.lineAt(lineNo)
    );

    const formattedLines: vscode.TextEdit[] = lines
        .map((line) => {
            const formattedLine: string = formatter(line.text, rules);
            return formattedLine !== line.text
                ? vscode.TextEdit.replace(line.range, formattedLine)
                : null;
        })
        .filter((e): e is vscode.TextEdit => e != null);
    return formattedLines;
};
