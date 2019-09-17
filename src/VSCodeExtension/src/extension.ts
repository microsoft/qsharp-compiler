// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { startTelemetry, EventNames, sendTelemetryEvent, reporter } from './telemetry';
import { DotnetInfo, requireDotNetSdk, findDotNetSdk } from './dotnet';
import { getPackageInfo } from './packageInfo';
import { installTemplates, createNewProject, registerCommand, openDocumentationHome, installOrUpdateIQSharp } from './commands';
import { LanguageServer } from './languageServer';

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

    let packageInfo = getPackageInfo(context);
    let dotNetSdkVersion = packageInfo === undefined ? undefined : packageInfo.requiredDotNetCoreSDK;

    // Get any .NET Core SDK version number to report in telemetry.
    var dotNetSdk : DotnetInfo | undefined;
    try {
        dotNetSdk = await findDotNetSdk();
    } catch {
        dotNetSdk = undefined;
    }

    sendTelemetryEvent(
        EventNames.activate,
        {
            'dotnetVersion': dotNetSdk !== undefined
                ? dotNetSdk.version
                : "<missing>"
        },
        {}
    );

    // Register commands that use the .NET Core SDK.
    // We do so as early as possible so that we can handle if someone calls
    // a command before we found the .NET Core SDK.
    registerCommand(
        context,
        "quantum.newProject",
        () => {
            requireDotNetSdk(dotNetSdkVersion).then(createNewProject);
        }
    );

    registerCommand(
        context,
        "quantum.installTemplates",
        () => {
            requireDotNetSdk(dotNetSdkVersion).then(
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
            requireDotNetSdk(dotNetSdkVersion).then(
                dotNetSdk => installOrUpdateIQSharp(
                    dotNetSdk,
                    packageInfo ? packageInfo.nugetVersion : undefined
                )
            );
        }
    );

    let rootFolder = findRootFolder();

    // let config = vscode.workspace.getConfiguration("quantumDevKit");
    // let configPath = config['languageServerPath'];
    // let languageServerPath =
    //     isAbsolute(configPath)
    //         ? configPath
    //         : context.asAbsolutePath(configPath);

    // Start the language server client.
    let languageServer = await LanguageServer.fromContext(context);
    if (languageServer === null) {
        // TODO: handle this error more gracefully by downloading the
        //       Q#LS blob.
        throw new Error("Could not find language server.");
    }
    let client = await languageServer.startClient(rootFolder);
    let disposable = client.start();

    console.log("[qsharp-lsp] Started LanguageClient object.");

    context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
export function deactivate() {
    if (reporter) { reporter.dispose(); }
}
