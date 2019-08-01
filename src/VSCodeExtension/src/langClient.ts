// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
import { LanguageClient, LanguageClientOptions, StreamInfo, State, CloseAction, ErrorAction, RevealOutputChannelOn } from 'vscode-languageclient';
import { DotNetSdk } from './dotnet';
import { ExtensionContext, workspace, Uri, Disposable } from 'vscode';
import { getPackageInfo } from './packageInfo';
import { isAbsolute } from 'path';
import { ChildProcess, spawn } from 'child_process';
import * as net from 'net';
import * as portfinder from 'portfinder';
import { tmpName, listenOnAvailablePort } from './utilities';
import * as url from 'url';
import { sendTelemetryEvent, EventNames, ErrorSeverities, forwardServerTelemetry } from './telemetry';


export class LanguageSession {
    private dotNetSdk: DotNetSdk;
    private assemblyPath: string;
    private rootFolder: string;
    private pushDisposable: (disposable: Disposable) => void;

    private constructor(
        dotNetSdk: DotNetSdk, assemblyPath: string, rootFolder: string,
        pushDisposable: (disposable: Disposable) => void
    ) {
        this.dotNetSdk = dotNetSdk;
        this.assemblyPath = assemblyPath;
        this.rootFolder = rootFolder;
        this.pushDisposable = pushDisposable;
    }

    public static async create(
        context: ExtensionContext,
        rootFolder: string
    ): Promise<LanguageSession> {
        // Find the .NET Core SDK we need for this client.
        let packageInfo = getPackageInfo(context);
        let dotNetSdkVersion =
            packageInfo === undefined
            ? undefined
            : packageInfo.requiredDotNetCoreSDK;
        let sdk = await DotNetSdk.require(dotNetSdkVersion);
        
        // Find the language server executable we need for
        // this client.
        let path =
            workspace.getConfiguration("quantumDevKit")["languageServerPath"];
        path =
            isAbsolute(path)
            ? path
            : context.asAbsolutePath(path);

        // Call the private constructor to make the new
        // language session.
        return new LanguageSession(sdk, path, rootFolder, context.subscriptions.push);
    }

    /**
     * Invokes a new process for the language server, using the `dotnet` tool provided with
     * the .NET Core SDK.
     *
     * @param port TCP port to use to talk to the language server.
     */
    private async spawnServerProcess(
        port : Number
    ) : Promise<ChildProcess>
    {
        // todo: try/catch and don't log if we couldn't find
        //       a log file.
        let logFile = await tmpName({
            prefix: "qsharp-",
            postfix: ".log"
        });
        
        // console.log(`[qsharp-lsp] Error finding temporary file for logging: ${err}.`);
        // args = args = [assemblyPath, `--port=${port}`]
        console.log(`[qsharp-lsp] Writing language server log to ${logFile}.`);
        let args = [
            this.assemblyPath,
            `--port=${port}`,
            `--log=${logFile}`
        ];

        let process = spawn(
            this.dotNetSdk.path,
            args,
            {
                cwd: this.rootFolder
            }
        ).on('error', err => {
            console.log(`[qsharp-lsp] Child process spawn failed with ${err}.`);
            throw err;
        }).on('exit', (exitCode, signal) => {
            console.log(`[qsharp-lsp] QsLanguageServer.exe exited with code ${exitCode} and signal ${signal}.`);
        });

        process.stderr.on('data', (data) => {
            console.error(`[qsharp-lsp] ${data}`);
        });
        process.stdout.on('data', (data) => {
            console.log(`[qsharp-lsp] ${data}`);
        });
        return process;
    }


    /**
     * Starts a language server process and returns a pair of streams
     * that can be used to communicate with the new language server.
     */
    private startServer(): Promise<StreamInfo> {
        return new Promise<StreamInfo>((resolve, reject) => {
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
                    return listenOnAvailablePort(server, port, port + 10, '127.0.0.1');
                })
                .then((actualPort) => {
                    console.log(`[qsharp-lsp] Successfully listening on port ${actualPort}, spawning server.`);
                    return this.spawnServerProcess(actualPort);
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

    public start(): LanguageClient {
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
                protocol2Code: str => Uri.parse(str)
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

        let client = new LanguageClient(
            'qsharp',
            'Q# Language Extension',
            () => this.startServer(),
            clientOptions
        );

        // Connect up events to the new client.
        this.pushDisposable(client.onTelemetry(forwardServerTelemetry));

        // The definition of the StateChangeEvent has changed in recent versions of VS Code,
        // so we use a dictionary from enum of different states to strings to make sure that
        // the debug log is useful even if the enum changes.
        this.pushDisposable(client.onDidChangeState(stateChangeEvent => {
            var states : { [key in State]: string } = {
                [State.Running]: "running",
                [State.Starting]: "starting",
                [State.Stopped]: "stopped"
            };
            console.log(`[qsharp-lsp] State ${states[stateChangeEvent.oldState]} -> ${states[stateChangeEvent.newState]}`);
        }));
        this.pushDisposable(client.onDidChangeState(
            (event) => {
                if (event.oldState === State.Running && event.newState === State.Stopped) {
                    sendTelemetryEvent(EventNames.lspStopped);
                }
            }
        ));

        let beganClientStartupAt = Date.now();            
        client
            .onReady()
            .then(() => {
                let elapsed = Date.now() - beganClientStartupAt;
                sendTelemetryEvent(
                    EventNames.lspReady,
                    {
                        'dotnetVersion': this.dotNetSdk.version
                    },
                    {
                        'elapsedTime': elapsed
                    }
                );
            });

        
        // We've provided the client with a function it can use to
        // get streams to and from a new server, and have connected up
        // events for when the client is ready.
        //
        // Now that we have everything wired up,
        // we can ask the language client to start itself.
        this.pushDisposable(client.start());

        return client;
    }

}
