// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

// IMPORTS /////////////////////////////////////////////////////////////////////

import * as vscode from 'vscode';
import * as os from 'os';
import * as path from 'path';
import * as fs from 'fs';
import * as net from 'net';

// import * as request from 'request';

// import * as which from 'which';
import * as cp from 'child_process';
import * as tmp from 'tmp';
// import * as semver from 'semver';

import * as portfinder from 'portfinder';

import { promisify } from 'util';
import { StreamInfo } from 'vscode-languageclient';

// EXPORTS /////////////////////////////////////////////////////////////////////

namespace CommonPaths {
    export const storageRelativePath = "server";
    export const executableNames = {
        darwin: "", // TODO
        linux: "", // TODO
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


export class LanguageServer {
    private path : string;
    private version : string;
    private constructor(path : string, version : string) {
        this.path = path;
        this.version = version;
        // TODO: remove. This is only here to mark version as used until we get that logic up and running.
        console.log(`Found Q# language server version ${this.version}`);
    }

    static async fromContext(context : vscode.ExtensionContext) : Promise<LanguageServer | null> {
        // TODO: allow overloading language server name from preferences.
        // Look at the global storage path for the context to try and find the
        // language server executable.
        let exeName = CommonPaths.executableNames[os.platform()];
        if (exeName === null) {
            throw new Error(`Unsupported platform: ${os.platform()}`);
        }
        let lsPath = path.join(context.globalStoragePath, CommonPaths.storageRelativePath, exeName);
        if (!await isPathExecutable(lsPath)) {
            // Language server didn't exist or wasn't executable.
            return null;
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
        return new LanguageServer(lsPath, response.stdout.trim());
    }

    /**
     * Invokes a new process for the language server, using the `dotnet` tool provided with
     * the .NET Core SDK.
     *
     * @param dotNetSdk Path to the .NET Core SDK.
     * @param assemblyPath Path to the language server assembly.
     * @param port TCP port to use to talk to the language server.
     * @param rootFolder Folder to use as the root for the new language server instance.
     */
    async spawnProcess(port : number, rootFolder : string) : Promise<cp.ChildProcess> {
        var args: string[];
        var logFile: string;
        try {
            logFile = await tmpName({
                prefix: "qsharp-",
                postfix: ".log"
            });
            console.log(`[qsharp-lsp] Writing language server log to ${logFile}.`);
            args = [`--port=${port}`, `--log=${logFile}`];
        } catch (err) {
            console.log(`[qsharp-lsp] Error finding temporary file for logging: ${err}.`);
            args = args = [`--port=${port}`];
        }

        let process = cp.spawn(
            this.path,
            args,
            {
                cwd: rootFolder
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
        process.stdout.on('data', (data) => {
            console.log(`[qsharp-lsp] ${data}`);
        });

        return process;
    }

    start(rootFolder : string): (() => Thenable<StreamInfo>) {
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
                    return this.spawnProcess(actualPort, rootFolder);
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
}
