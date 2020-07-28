// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as vscode from "vscode";
import { FormatRule, formatter } from "./formatter";
import indentRules from "./rules/indent";
import operationRules from "./rules/declaration";
import controlStructureRules from "./rules/control-structure";

const rules: FormatRule[] = [
    ...indentRules,
    ...operationRules,
    ...controlStructureRules,
];

export const formatDocument = (
    document: vscode.TextDocument
): vscode.TextEdit[] => {
    const firstLine = document.lineAt(0);
    const lastLine = document.lineAt(document.lineCount - 1);
    const range: vscode.Range = new vscode.Range(firstLine.range.start, lastLine.range.end);

    const formattedDocument: string = formatter(document.getText(), rules);

    return [vscode.TextEdit.replace(range, formattedDocument)];
};
