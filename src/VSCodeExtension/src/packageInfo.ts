// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

// IMPORTS /////////////////////////////////////////////////////////////////////

import * as vscode from 'vscode';

// EXPORTS /////////////////////////////////////////////////////////////////////

export interface IPackageInfo {
    name: string;
    version: string;
    aiKey: string;
    requiredDotNetCoreSDK: string;
}

export function getPackageInfo(context: vscode.ExtensionContext): IPackageInfo | undefined {
    let extensionPackage = require(context.asAbsolutePath('./package.json'));
    if (extensionPackage) {
        return {
            name: extensionPackage.name,
            version: extensionPackage.version,
            aiKey: extensionPackage.aiKey,
            requiredDotNetCoreSDK: extensionPackage.requiredDotNetCoreSDK
        };
    }
    return;
}
