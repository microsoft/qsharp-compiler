// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

// IMPORTS /////////////////////////////////////////////////////////////////////

import * as vscode from 'vscode';
import * as os from 'os';
import * as path from 'path';
import * as fs from 'fs';
// import * as request from 'request';

// import * as which from 'which';
import * as cp from 'child_process';
import * as tmp from 'tmp';
// import * as semver from 'semver';

import { promisify } from 'util';

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
    async spawn(port : number, rootFolder : string) : Promise<cp.ChildProcess> {
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
}
