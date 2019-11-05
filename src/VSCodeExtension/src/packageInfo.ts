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
    enableTelemetry: string;
    nugetVersion: string;
    assemblyVersion: string;
    blobs?: {
        [key: string]: {
            url: string,
            sha256: string,
            size?: number
        }
    };
}

export function getPackageInfo(context: vscode.ExtensionContext): IPackageInfo | undefined {
    let extensionPackage = require(context.asAbsolutePath('./package.json'));
    if (extensionPackage) {
        return {
            name: extensionPackage.name,
            version: extensionPackage.version,
            aiKey: extensionPackage.aiKey,
            requiredDotNetCoreSDK: extensionPackage.requiredDotNetCoreSDK,
            enableTelemetry: extensionPackage.enableTelemetry,
            nugetVersion: extensionPackage.nugetVersion,
            assemblyVersion: extensionPackage.assemblyVersion,
            blobs: extensionPackage.blobs
        };
    }
    return;
}
