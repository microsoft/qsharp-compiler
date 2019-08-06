// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

import * as vscode from 'vscode';
import { startTelemetry, EventNames, sendTelemetryEvent, reporter, ErrorSeverities } from './telemetry';
import { DotNetSdk } from './dotnet';
import { getPackageInfo } from './packageInfo';
import { installTemplates, createNewProject, registerCommand, openDocumentationHome, installOrUpdateIQSharp, launchJupyterNotebook } from './commands';
import { LanguageSession } from './langClient';

/**
 * Returns the root folder for the current workspace.
 */
function findRootFolder() : string {
    // FIXME: handle multiple workspace folders here.
    let workspaceFolders = vscode.workspace.workspaceFolders;
    if (workspaceFolders) {
        return workspaceFolders[0].uri.fsPath;
    } else {
        return '';
    }
}

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export async function activate(context: vscode.ExtensionContext) {

    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    console.log('[qsharp-lsp] Activated!');
    process.env['VSCODE_LOG_LEVEL'] = 'trace';

    startTelemetry(context);
    sendTelemetryEvent(EventNames.activate, {}, {});

    let packageInfo = getPackageInfo(context);
    let dotNetSdkVersion =  packageInfo === undefined ? undefined : packageInfo.requiredDotNetCoreSDK;
    
    // Register commands that use the .NET Core SDK.
    // We do so as early as possible so that we can handle if someone calls
    // a command before we found the .NET Core SDK.
    registerCommand(
        context,
        "quantum.newProject",
        () => {
            DotNetSdk.require(dotNetSdkVersion).then(createNewProject)
        }
    );

    registerCommand(
        context,
        "quantum.installTemplates",
        () => {
            DotNetSdk.require(dotNetSdkVersion).then(
                dotNetSdk => installTemplates(dotNetSdk, packageInfo)
            );
        }
    );

    registerCommand(
        context,
        "quantum.openDocumentation",
        openDocumentationHome
    );

    registerCommand(
        context,
        "quantum.installIQSharp",
        () => {
            DotNetSdk.require(dotNetSdkVersion).then(
                dotNetSdk => installOrUpdateIQSharp(
                    dotNetSdk,
                    packageInfo ? packageInfo.nugetVersion : undefined
                )
            );
        }
    );

    registerCommand(
        context,
        "quantum.launchJupyter",
        launchJupyterNotebook
    );


    let dotNetSdk: DotNetSdk;
    try {
        dotNetSdk = await DotNetSdk.require(
            packageInfo === undefined ? undefined : packageInfo.requiredDotNetCoreSDK
        );
    } catch (error) {
        sendTelemetryEvent(EventNames.error, {
            id: "dotnet-missing", 
            severity: ErrorSeverities.Error,
            reason: error.message
        });
        console.log(`[qsharp-lsp] Could not find .NET Core SDK: ${error}`);
        return;
    }
    console.log(`[qsharp-lsp] Found the .NET Core SDK at ${dotNetSdk.path}.`);

    // Start the language server client.
    let rootFolder = findRootFolder();
    await
        LanguageSession
        .create(context, rootFolder)
        .then(session => session.start());
}

// this method is called when your extension is deactivated
export function deactivate() {
    if (reporter) { reporter.dispose(); }
}
