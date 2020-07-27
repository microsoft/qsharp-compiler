// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

import * as vscode from 'vscode';

const formatLine = (line: string, indent: number = 0): string => {
    line = line.trimRight();
    // Left-align namespace declaration
    if (/\s+(namespace)/.test(line)) {
        line = line.trimLeft();
    }
    // Remove whitespace in arguments
    line = line.replace(/((\w*)\s*:\s*([a-zA-Z]+))/g, (match, p1, v, t, offset, string) => {
        return `${v}: ${t}`;
    })
    return line;
};

export function formatDocument(document: vscode.TextDocument): vscode.TextEdit[] {
    let indent = 0;
    // Formats each line one-by-one
    return Array.from(Array(document.lineCount), (_, lineNo) => {
        const line = document.lineAt(lineNo);
        const formatted = formatLine(line.text, indent);
        if (formatted == line.text) return null;
        return vscode.TextEdit.replace(line.range, formatted);
    }).filter((e): e is vscode.TextEdit => e != null);
}
