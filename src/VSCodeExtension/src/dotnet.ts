// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

// IMPORTS /////////////////////////////////////////////////////////////////////

import * as vscode from 'vscode';

import * as which from 'which';
import * as cp from 'child_process';
import * as semver from 'semver';
import { promisify } from 'util';

// EXPORTS /////////////////////////////////////////////////////////////////////

export type DotnetInfo = {path: string, version: string};

let dotnet : DotNetSdk | undefined = undefined;

let exec = promisify(cp.exec);

export class DotNetSdk {
    private info: DotnetInfo;

    constructor(info: DotnetInfo) {
        this.info = info;
    }

    get path(): string {
        return this.info.path;
    }

    get version(): string {
        return this.info.version;
    }

    /**
     * exec
     */
    public exec(cmd: string, options?: cp.ExecOptions): Promise<{
        stdout: string | Buffer,
        stderr: string | Buffer
    }> {
        return exec(
            `"${this.info.path} ${cmd}`,
            options
        );
    }

    /**
    * Returns the path to the .NET Core SDK, or prompts the user to install
    * if the SDK is not found.
    */
    public static find() : Promise<DotNetSdk> {
        return new Promise((resolve, reject) => {
            if (dotnet === undefined) {
                try {
                    let path = which.sync("dotnet");
                    exec(
                        `"${path}" --version`
                    ).then(
                        value => {
                            dotnet = new DotNetSdk({path: path, version: value.stdout.trim()});
                            resolve(dotnet);
                        }
                    ).catch(reject);
                } catch (ex) {
                    DotNetSdk.promptToInstall("The .NET Core SDK was not found on your PATH.");
                    reject(ex);
                }
            } else {
                resolve(dotnet);
            }

        });
    }

    public static require(version? : string) : Promise<DotNetSdk> {
        return new Promise((resolve, reject) => {
            DotNetSdk.find()
                .then(
                    dotnet => {
                        if (version !== undefined && semver.lt(dotnet.version, version)) {
                            let msg = `The Quantum Development Kit extension requires .NET Core SDK version ${version} or later, but ${dotnet.version} was found.`;
                            DotNetSdk.promptToInstall(msg);
                            reject(msg);
                        }
                        resolve(dotnet);
                    }
                );
        });
    }

    public static promptToInstall(msg : string) {
        let installItem = "Install .NET Core SDK...";
        vscode.window
            .showErrorMessage(msg, installItem)
            .then(
                (item) => {
                    if (item === installItem) {
                        vscode.env.openExternal(vscode.Uri.parse("https://dotnet.microsoft.com/download"));
                    }
                }
            );
    }
    
}


export function findIQSharpVersion() : Promise<{[key: string]: string} | undefined> {
    return new Promise((resolve, reject) => {
        DotNetSdk.require().then(
            dotnet => {
                cp.exec(
                    `"${dotnet.path}" iqsharp --version`,
                    (error, stdout, stderr) => {
                        if (error === null) {
                            let components = stdout.split("\n");
                            let componentVersions: {[key: string]: string} = {};
                            components.forEach(
                                component => {
                                    if (component.trim() === "") { return; }
                                    let parts = component.split(":", 2);
                                    componentVersions[parts[0].trim()] = parts[1].trim();
                                }
                            );
                            resolve(componentVersions);
                        } else if (
                            error !== null &&
                            error.message.indexOf("No executable found matching command \"dotnet-iqsharp\"") !== -1
                        ) {
                            // If the .NET Core SDK cannot find IQ#, we'll get an error
                            // of the form:
                            //     Command failed: "C:\Program Files\dotnet\dotnet.EXE" iqsharp --version
                            //     No executable found matching command "dotnet-iqsharp"
                            // Thus, we can look for the string above to tell if .NET Core SDK
                            // couldn't find IQ#.
                            // In that case, the version is undefined.
                            resolve(undefined);
                        } else {
                            reject(stderr);
                        }
                    }
                );
            }
        );
    });
}
