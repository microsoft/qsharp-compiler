// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

// IMPORTS /////////////////////////////////////////////////////////////////////

import * as vscode from 'vscode';

import * as which from 'which';
import * as cp from 'child_process';
import * as semver from 'semver';

// EXPORTS /////////////////////////////////////////////////////////////////////

export type DotnetInfo = {path: string, version: string};

let dotnet : DotnetInfo | undefined = undefined;

function promptToInstallDotNetCoreSDK(msg : string) {
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

/**
 * Returns the path to the .NET Core SDK, or prompts the user to install
 * if the SDK is not found.
 */
export function findDotNetSdk() : Promise<DotnetInfo> {
    return new Promise((resolve, reject) => {

        if (dotnet === undefined) {
            try {
                let path = which.sync("dotnet");
                cp.exec(
                    `"${path}" --version`,
                    (error, stdout, stderr) => {
                        if (error === null) {
                            dotnet = {path: path, version: stdout.trim()};
                            resolve(dotnet);
                        } else {reject(error);}
                    }
                );
            } catch (ex) {
                promptToInstallDotNetCoreSDK("The .NET Core SDK was not found on your PATH.");
                reject(ex);
            }
        } else {
            resolve(dotnet);
        }

    });
}

export function requireDotNetSdk(version? : string) : Promise<DotnetInfo> {
    return new Promise((resolve, reject) => {
        findDotNetSdk()
            .then(
                dotnet => {
                    if (version !== undefined && semver.lt(dotnet.version, version)) {
                        let msg = `The Quantum Development Kit extension requires .NET Core SDK version ${version} or later, but ${dotnet.version} was found.`;
                        promptToInstallDotNetCoreSDK(msg);
                        reject(msg);
                    }
                    resolve(dotnet);
                }
            );
    });
}

export function findIQSharpVersion() : Promise<{[key: string]: string} | undefined> {
    return new Promise((resolve, reject) => {
        requireDotNetSdk().then(
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
