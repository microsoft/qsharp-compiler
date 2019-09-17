// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, State, CloseAction, ErrorAction, RevealOutputChannelOn } from 'vscode-languageclient';

import { isAbsolute } from 'path';
import * as url from 'url';

import { startTelemetry, EventNames, sendTelemetryEvent, reporter, ErrorSeverities, forwardServerTelemetry } from './telemetry';
import { DotnetInfo, requireDotNetSdk, findDotNetSdk } from './dotnet';
import { getPackageInfo } from './packageInfo';
import { installTemplates, createNewProject, registerCommand, openDocumentationHome, installOrUpdateIQSharp } from './commands';
import { LanguageServer } from './languageServer';

const extensionStartedAt = Date.now();

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
            requireDotNetSdk(dotNetSdkVersion).then(createNewProject)
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

    let config = vscode.workspace.getConfiguration("quantumDevKit");
    let configPath = config['languageServerPath'];
    let languageServerPath =
        isAbsolute(configPath)
            ? configPath
            : context.asAbsolutePath(configPath);

    // Get any .NET Core SDK version number to report.
    var dotNetSdk : DotnetInfo | undefined;
    try {
        dotNetSdk = await findDotNetSdk();
    } catch {
        dotNetSdk = undefined;
    }

    // Start the language server client.
    let languageServer = await LanguageServer.fromContext(context);
    if (languageServer === null) {
        // TODO: handle this error more gracefully by downloading the
        //       Q#LS blob.
        throw new Error("Could not find language server.");
    }
    let serverOptions: ServerOptions = languageServer.start(rootFolder);

    let clientOptions: LanguageClientOptions = {
        initializationOptions: {
            client: "VSCode",
        },
        documentSelector: [
            {scheme: "file", language: "qsharp"}
        ],
        revealOutputChannelOn: RevealOutputChannelOn.Never,

        // Due to the known issue
        // https://github.com/Microsoft/vscode-languageserver-node/issues/105,
        // we use the workaround from
        // https://github.com/felixfbecker/vscode-php-intellisense/pull/23
        // to convert URIs in and out of VS Code's internal formatting through
        // the "uri" NPM package.
        uriConverters: {
            // VS Code by default %-encodes even the colon after the drive letter
            // NodeJS handles it much better
            code2Protocol: uri => url.format(url.parse(uri.toString(true))),
            protocol2Code: str => vscode.Uri.parse(str)
        },

        errorHandler: {
            closed: () => {
                return CloseAction.Restart;
            },
            error: (error, message, count) => {
                sendTelemetryEvent(EventNames.error, {
                    id: error.name,
                    severity: ErrorSeverities.Error,
                    reason: error.message
                });
                // By default, continue the server as best as possible.
                return ErrorAction.Continue;
            }
        }
    };

    console.log(`[qsharp-lsp] Language server assembly: ${languageServerPath}`);

    let client = new LanguageClient(
        'qsharp',
        'Q# Language Extension',
        serverOptions,
        clientOptions
    );
    context.subscriptions.push(
        // The definition of the StateChangeEvent has changed in recent versions of VS Code,
        // so we use a dictionary from enum of different states to strings to make sure that
        // the debug log is useful even if the enum changes.
        client.onDidChangeState(stateChangeEvent => {
            var states : { [key in State]: string } = {
                [State.Running]: "running",
                [State.Starting]: "starting",
                [State.Stopped]: "stopped"
            };
            console.log(`[qsharp-lsp] State ${states[stateChangeEvent.oldState]} -> ${states[stateChangeEvent.newState]}`);
        })
    );

    // When we're ready, send the telemetry event that we started successfully.
    client
        .onReady().then(() => {
            let elapsed = Date.now() - extensionStartedAt;
            sendTelemetryEvent(
                EventNames.lspReady,
                {
                    'dotnetVersion': dotNetSdk !== undefined
                                        ? dotNetSdk.version
                                        : "<missing>"
                },
                {
                    'elapsedTime': elapsed
                }
            );
        });
    client.onDidChangeState(
        (event) => {
            if (event.oldState === State.Running && event.newState === State.Stopped) {
                sendTelemetryEvent(EventNames.lspStopped);
            }
        }
    );
    client.onTelemetry(forwardServerTelemetry);
    let disposable = client.start();

    console.log("[qsharp-lsp] Started LanguageClient object.");

    context.subscriptions.push(disposable);
}

// this method is called when your extension is deactivated
export function deactivate() {
    if (reporter) { reporter.dispose(); }
}
