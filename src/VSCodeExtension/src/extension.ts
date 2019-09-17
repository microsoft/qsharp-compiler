// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { LanguageClient, LanguageClientOptions, ServerOptions, StreamInfo, State, CloseAction, ErrorAction, RevealOutputChannelOn } from 'vscode-languageclient';

import * as net from 'net';
import * as portfinder from 'portfinder';
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

/**
 * Given a server, attempts to listen on a given port, incrementing the port
 * number on failure, and yielding the actual port that was used.
 *
 * @param server The server that will be listened on.
 * @param port The first port to try listening on.
 * @param maxPort The highest port number before considering the promise a
 *     failure.
 * @param hostname The hostname that the server should listen on.
 * @returns A promise that yields the actual port number used, or that fails
 *     when net.Server yields an error other than EADDRINUSE or when all ports
 *     up to and including maxPort are already in use.
 */
function listenPromise(server: net.Server, port: number, maxPort: number, hostname: string): Promise<number> {
    return new Promise((resolve, reject) => {
        if (port >= maxPort) {
            reject("Could not find an open port.");
        }
        server.listen(port, hostname)
            .on('listening', () => resolve(port))
            .on('error', (err) => {
                // The 'error' callback lists that err has type Error, which
                // is not specific enough to ensure that the property "code"
                // exists. We cast through any to work around this typing
                // bug, but that's not good at all.
                //
                // To try and mitigate the impact of casting through any,
                // we check explicitly if err.code exists first. In the case
                // that it doesn't, we fail through with err as intended.
                //
                // See
                //     https://github.com/angular/angularfire2/issues/666
                // for another example of a very similar bug.
                if ("code" in err && (err as any).code === "EADDRINUSE") {
                    // portfinder accidentally gave us a port that was already in use,
                    // which can happen due to race conditions. Let's try the next few
                    // ports in case we get lucky.
                    resolve(listenPromise(server, port + 1, maxPort, hostname));
                }
                // If we got any other error, reject the promise here; there's
                // nothing else we can do.
                reject(err);
            });
    });
}

function startServer(languageServer : LanguageServer, rootFolder : string): (() => Thenable<StreamInfo>) {
    return () => new Promise((resolve, reject) => {

        let server = net.createServer(socket => {
            // We use an explicit cast here as a workaround for a bug in @types/node.
            // https://github.com/DefinitelyTyped/DefinitelyTyped/issues/17020
            resolve({
                reader: socket,
                writer: socket
            } as StreamInfo);
        });

        // Begin by trying to find an appropriate port to pass along to the LSP executable.
        portfinder.getPortPromise({'port': 8091})
            .then(port => {
                console.log(`[qsharp-lsp] Found port at ${port}.`);
                // We found a port, so let's go along and use it to
                // make a socket server.
                return listenPromise(server, port, port + 10, '127.0.0.1');
            })
            .then((actualPort) => {
                console.log(`[qsharp-lsp] Successfully listening on port ${actualPort}, spawning server.`);
                return languageServer.spawn(actualPort, rootFolder);
            })
            .then((childProcess) => {
                console.log(`[qsharp-lsp] started QsLanguageServer.exe as PID ${childProcess.pid}.`);
            })
            .catch(err => {
                // Could not find a port...
                console.log(`[qsharp-lsp] Could not open an unused port: ${err}.`);
                reject(err);
            });

    });
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
        //       Q#LS blog.
        throw new Error("Could not find language server.");
    }
    let serverOptions: ServerOptions =
        startServer(languageServer, rootFolder);

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
