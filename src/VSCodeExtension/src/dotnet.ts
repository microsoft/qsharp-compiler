// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

// IMPORTS /////////////////////////////////////////////////////////////////////

import * as vscode from 'vscode';

// opn is not ES6 compliant.
import opn = require('opn');
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
                    opn("https://dotnet.microsoft.com/download");
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

export function requireDotNetSdk(version : string | undefined) : Promise<DotnetInfo> {
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