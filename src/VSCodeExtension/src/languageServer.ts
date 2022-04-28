// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

// IMPORTS /////////////////////////////////////////////////////////////////////

import * as vscode from 'vscode';
import * as os from 'os';
import * as path from 'path';
import * as fs from 'fs-extra';
import * as url from 'url';
import * as crypto from 'crypto';

// import * as request from 'request';

// import * as which from 'which';
import * as cp from 'child_process';
import * as tmp from 'tmp';
// import * as semver from 'semver';


import { promisify } from 'util';
import { LanguageClient, ServerOptions, RevealOutputChannelOn, LanguageClientOptions, CloseAction, ErrorAction, State } from 'vscode-languageclient/node';
import { sendTelemetryEvent, EventNames, ErrorSeverities, forwardServerTelemetry } from './telemetry';

import DecompressZip = require('decompress-zip');
import { getPackageInfo } from './packageInfo';
import request = require('request');

// EXPORTS /////////////////////////////////////////////////////////////////////

namespace CommonPaths {
    export const storageRelativePath = "server";
    export const executableNames = {
        darwin: "Microsoft.Quantum.QsLanguageServer",
        linux: "Microsoft.Quantum.QsLanguageServer",
        win32: "Microsoft.Quantum.QsLanguageServer.exe",
        aix: null,
        android: null,
        freebsd: null,
        openbsd: null,
        sunos: null,
        cygwin: null
    };
}

function isPathExecutable(path : string) : Promise<boolean> {
    return new Promise((resolve, reject) => {
        fs.access(path, fs.constants.X_OK, err => {
            if (err) {
                resolve(false);
            } else {
                resolve(true);
            }
        });
    });
}

function tmpName(config : tmp.SimpleOptions) : Promise<string> {
    return new Promise<string>((resolve, reject) => {
        tmp.tmpName(config, (err, path) => {
            if (err) {
                reject(err);
            } else {
                resolve(path);
            }
        });
    });
}

function sha256sum(path : string) : Promise<string> {
    return new Promise((resolve, reject) => {
        // Node.js hashes are an odd kind of stream, see
        // https://stackoverflow.com/a/18658613 for an example.
        // What we need to do is set up the events on the file read stream
        // to end the hash stream when it ends, so that pipe does everything
        // for us automatically.
        var readStream = fs.createReadStream(path);
        var hash = crypto.createHash('sha256');
        hash.setEncoding('hex');

        readStream
            .on('end', () => {
                hash.end();
                resolve(hash.read().toString());
            })
            .on('error', reject);

        readStream.pipe(hash);
    });
}

export class LanguageServer {
    private serverExe : {
        path : string,
        version : string
    } | undefined;
    private context : vscode.ExtensionContext;
    private rootFolder : string;

    constructor(context : vscode.ExtensionContext, rootFolder : string) {
        this.context = context;
        this.rootFolder = rootFolder;
    }

    async clearCache() : Promise<void> {
        let cachePath = path.join(this.context.globalStoragePath, CommonPaths.storageRelativePath);
        return new Promise<void>((resolve, reject) => {
            fs.remove(cachePath, err => {
                if (err) {
                    reject(err);
                } else {
                    resolve();
                }
            });
        });
    }

    async findExecutable() : Promise<boolean> {
        let lsPath : string | undefined | null = undefined;
        // Before anything else, look at the user's configuration to see
        // if they set a path manually for the language server.
        let versionCheck = false;
        let config = vscode.workspace.getConfiguration();
        lsPath = config.get("quantumDevKit.languageServerPath");

        // If lsPath is still undefined or null, then we didn't have a manual
        // path set up above.
        if (lsPath === undefined || lsPath === null || (lsPath.trim() === "")) {
            // Look at the global storage path for the context to try and find the
            // language server executable.
            let exeName = CommonPaths.executableNames[os.platform()];
            if (exeName === null) {
                throw new Error(`Unsupported platform: ${os.platform()}`);
            }
            lsPath = path.join(this.context.globalStoragePath, CommonPaths.storageRelativePath, exeName);
            versionCheck = true;
        }

        // Since lsPath has been set unconditionally, we can now proceed to
        // check if it's valid or not.
        if (!await isPathExecutable(lsPath)) {
            console.log(`[qsharp-lsp] "${lsPath}" is not executable. Proceed to download Q# language server.`)
            // Language server didn't exist or wasn't executable.
            return false;
        }
        // NB: There is a possible race condition here, as per node docs. An
        //     alternative approach might be to simply run the language server
        //     and catch errors there.
        var response : {stdout: string, stderr: string};
        try {
            response = await promisify(cp.exec)(`"${lsPath}" --version`);
        } catch (err) {
            console.log(`[qsharp-lsp] Error while fetching LSP version: ${err}`);
            throw err;
        }

        if (response.stderr.trim().length !== 0) {
            throw new Error(`Language server returned error when reporting version: ${response.stderr}`);
        }

        let version = response.stdout.trim();
        let info = getPackageInfo(this.context);
        if (info === undefined || info === null) {
            throw new Error("Package info was undefined.");
        }
        console.log(`[qsharp-lsp] Package is ${info.name} with ${info.nugetVersion} and ${info.version}`);
        if (versionCheck && info.nugetVersion !== version) {
            console.log(`[qsharp-lsp] Found version ${version}, expected version ${info.nugetVersion}. Clearing cached version.`);
            await this.clearCache();
            return false;
        }

        this.serverExe = {
            path: lsPath,
            version: version
        };
        return true;
    }

    private async setAsExecutable(path : string) : Promise<void> {
        let results = await promisify(cp.exec)(`chmod +x "${path}"`);
        console.log(`[qsharp-lsp] Results from setting ${path} as executable:\n${results.stdout}\nstderr:\n${results.stderr}`);
        return;
    }

    private decompressServerBlob(blobPath : string) : Thenable<void> {
        console.log(`[qsharp-lsp] Decompressing ${blobPath}.`);
        return vscode.window.withProgress(
            {
                cancellable: false,
                location: vscode.ProgressLocation.Window,
                title: "Unpacking Q# language server"
            },
            (progress, token) => {
                return new Promise((resolve, reject) => {
                    let unzipper = new DecompressZip(blobPath);
                    let targetPath = path.join(this.context.globalStoragePath, CommonPaths.storageRelativePath);
                    unzipper.on('progress', (index, count) => {
                        progress.report({
                            message: `File ${index} of ${count}...`
                        });
                    });
                    unzipper.on('error', err => {
                        console.log(`[qsharp-lsp] Error while decompressing language server blog: ${err}`);
                        reject(err);
                    });
                    unzipper.on('extract', log => {
                        console.log(log);
                        // If we're on Windows, we're done. On Unix-like
                        // systems, though, we need to mark the server as
                        // executable first.
                        if (os.platform() === "win32") {
                            resolve();
                        } else {
                            let relativeExecutablePath = CommonPaths.executableNames[os.platform()];
                            this.setAsExecutable(
                                path.join(
                                    targetPath,
                                    relativeExecutablePath!
                                )
                            ).then(resolve);
                        }
                    });
                    unzipper.extract({
                        path: targetPath
                    });
                });
            }
        );
    }

    async downloadLanguageServer() : Promise<void> {
        let info = getPackageInfo(this.context);
        if (info === undefined || info.blobs === undefined) {
            throw new Error("Package info did not contain information about language server blobs.");
        }
        let blob = info.blobs[os.platform()];
        let downloadTarget = await tmpName({postfix: ".zip"});
        console.log(`[qsharp-lsp] Downloading ${blob.url} to ${downloadTarget}.`);
        await vscode.window.withProgress(
            {
                cancellable: false,
                location: vscode.ProgressLocation.Window,
                title: "Downloading Q# language server"
            },
            (progress, token) => {
                var bytesReceived = 0;
                return new Promise((resolve, reject) => {
                    request
                        .get(blob.url)
                        .on('error', reject)
                        .on('data', data => {
                            bytesReceived += data.length;
                            let ofMessage = blob.size
                                ? `/ ${blob.size}`
                                : "";
                            progress.report({
                                message: `${bytesReceived}${ofMessage} bytes...`
                            });
                        })
                        .pipe(fs.createWriteStream(downloadTarget))
                        .on('close', resolve);
                });
            }
        );
        if (blob.sha256 !== "<DISABLED>") {
            let digest = await sha256sum(downloadTarget);
            if (digest.localeCompare(blob.sha256, [], { sensitivity: 'base' })  !== 0) {
                let reason = `Expected SHA256 sum ${blob.sha256}, got ${digest}.`;
                console.log(`[qsharp-lsp] ${reason}`);
                throw new Error(reason);
            } else {
                console.log(`[qsharp-lsp] Done downloading. Got expected SHA256 sum: ${digest}.`);
            }
        }
        try {
            await this.decompressServerBlob(downloadTarget);
            console.log("[qsharp-lsp] Done decompressing, language server should be there.");
        } catch (err) {
            console.log(err);
            throw new Error(`${err}`);
        } finally {
            console.log("[qsharp-lsp] Removing downloaded ZIP.");
            fs.remove(downloadTarget);
        }
    }

    /**
     * Invokes a new process for the language server, using the executable found
     * by the findExecutable method.
     *
     * @param port TCP port to use to talk to the language server.
     */
    async spawnProcess() : Promise<cp.ChildProcess> {
        if (this.serverExe === undefined) {
            // Try to find the exe, and fail out if not found.
            if (!await this.findExecutable()) {
                throw new Error("Could not find language server executable to spawn.");
            } else {
                // Assert that the exe info is there so that TypeScript is happy;
                // this should be post-condition of findExecutable returning true.
                console.assert(this.serverExe !== undefined, "Language server path not set after successfully finding executable. This should never happen.");
                this.serverExe = this.serverExe!;
            }
        }
        var args: string[];
        var logFile: string;
        try {
            logFile = await tmpName({
                prefix: "qsharp-",
                postfix: ".log"
            });
            console.log(`[qsharp-lsp] Writing language server log to ${logFile}.`);
            args = [`--stdinout`, `--log=${logFile}`];
        } catch (err) {
            console.log(`[qsharp-lsp] Error finding temporary file for logging: ${err}.`);
            args = args = [`--stdinout`]
        }

        let process = cp.spawn(
            this.serverExe.path,
            args,
            {
                cwd:  this.rootFolder,
                stdio: 'pipe'
            }
        ).on('error', err => {
            console.log(`[qsharp-lsp] Child process spawn failed with ${err}.`);
            throw(err);
        }).on('exit', (exitCode, signal) => {
            console.log(`[qsharp-lsp] QsLanguageServer.exe exited with code ${exitCode} and signal ${signal}.`);
        });

        process.stderr.on('data', (data) => {
            console.error(`[qsharp-lsp] ${data}`);
        });

        return process;
    }

    private startOptions(): ServerOptions {
        return () => new Promise((resolve, reject) => {

            this.spawnProcess()
                .then((process) => {
                    resolve({
                        reader: process.stdout,
                        writer: process.stdin
                    });
                })
                .catch(err => {
                    // Could not spawn process
                    console.log(`[qsharp-lsp] Could not start language server: ${err}.`);
                    reject(err);
                });

        });
    }

    private async startClient() : Promise<LanguageClient> {
        const languageServerStartedAt = Date.now();
        let serverOptions = this.startOptions();

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

        let client = new LanguageClient(
            'qsharp',
            'Q# Language Extension',
            serverOptions,
            clientOptions
        );
        this.context.subscriptions.push(
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

        client.onDidChangeState(
            (event) => {
                if (event.oldState === State.Running && event.newState === State.Stopped) {
                    sendTelemetryEvent(EventNames.lspStopped);
                }
            }
        );
        client.onTelemetry(forwardServerTelemetry);

        // When we're ready, send the telemetry event that we started successfully.
        client.onReady().then(() => {
            let elapsed = Date.now() - languageServerStartedAt;
            sendTelemetryEvent(
                EventNames.lspReady,
                {},
                {
                    'elapsedTime': elapsed
                }
            );
        });

        return client;

    }

    async start(retries : number = 3) : Promise<void> {
        if (!await this.findExecutable()) {
            // Try again after downloading.
            try {
                await this.downloadLanguageServer();
            } catch (err) {
                console.log(`[qsharp-lsp] Error downloading language server: ${err}. ${retries} left.`);
                if (retries > 0) {
                    return this.start(retries - 1);
                } else {
                    let retryItem = "Try again";
                    let reportFeedbackItem = "Report feedback...";
                    switch (await vscode.window.showErrorMessage(
                        "Could not download Q# language server.",
                        retryItem, reportFeedbackItem
                    )) {
                        case retryItem:
                            return this.start(1);
                            break;
                        case reportFeedbackItem:
                            vscode.env.openExternal(vscode.Uri.parse(
                                "https://github.com/microsoft/qsharp-compiler/issues/new?assignees=&labels=bug,Area-IDE&template=bug_report.md&title="
                            ));
                            return;
                            break;
                    }
                }
            }
            if (!this.findExecutable()) {
                // NB: This case should never occur, as we only get to this
                //     point in control flow by passing the try/catch above
                //     without throwing an error. If we get here something
                //     seriously wrong has occurred inside the extension.
                throw new Error("Could not find language server.");
            }
        }
        let client = await this.startClient();
        let disposable = client.start();
        this.context.subscriptions.push(disposable);

        console.log("[qsharp-lsp] Started LanguageClient object.");
    }
}
