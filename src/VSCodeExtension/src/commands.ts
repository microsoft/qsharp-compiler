// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
import * as vscode from 'vscode';
import * as cp from 'child_process';

import { DotnetInfo, findIQSharpVersion } from './dotnet';
import { IPackageInfo } from './packageInfo';
import * as semver from 'semver';
import { promisify } from 'util';
import { oc } from 'ts-optchain';
import { QSharpGenerator } from './yeoman-generator';

import * as yeoman from 'yeoman-environment';

export function registerCommand(context: vscode.ExtensionContext, name: string, action: () => void) {
    context.subscriptions.push(
        vscode.commands.registerCommand(
            name,
            () => {
                action();
            }
        )
    )
}

export function createNewProject(context: vscode.ExtensionContext) {    
    let env = yeoman.createEnv();
    env.options.extensionPath = context.extensionPath;
    env.registerStub(QSharpGenerator, 'qsharp:app');
    env.run('qsharp:app', (err: null | Error) => {
        if (err) {
            let errorMessage = err.name + ": " + err.message;
            console.log(errorMessage);
            vscode.window.showErrorMessage(errorMessage);
        }
    });
}

export function installTemplates(dotNetSdk: DotnetInfo, packageInfo?: IPackageInfo) {
    let packageVersion =
        oc(packageInfo).nugetVersion()
        ? `::${packageInfo!.nugetVersion}`
        : "";
    let proc = cp.spawn(
        dotNetSdk.path,
        ["new", "--install", `Microsoft.Quantum.ProjectTemplates${packageVersion}`]
    );
    
    let errorMessage = "";
    proc.stderr.on(
        'data', data => {
            errorMessage = errorMessage + data;
        }
    );
    proc.stdout.on(
        'data', data => {
            console.log("" + data);
        }        
    )

    proc.on(
        'exit',
        (code, signal) => {
            console.log("dotnet new --install stderr:", errorMessage);
            if (code === 0) {
                vscode.window.showInformationMessage(
                    "Project templates installed successfully."
                );
            } else {
                vscode.window.showErrorMessage(
                    `.NET Core SDK exited with code ${code} when installing project templates:\n${errorMessage}`
                );
            }
        }
    );
}

export function openDocumentationHome() {
    return vscode.env.openExternal(
        vscode.Uri.parse("https://docs.microsoft.com/quantum/")
    );
}

export function installOrUpdateIQSharp(dotNetSdk: DotnetInfo, requiredVersion?: string) {
    findIQSharpVersion()
        .then(
            iqsharpVersion => {
                if (iqsharpVersion !== undefined && iqsharpVersion["iqsharp"] !== undefined) {
                    // We got a version, so let's check if it's up to date or not.
                    // If it is up to date, we print out that this is the case and resolve the
                    // promise immediately.
                    if (requiredVersion === undefined || semver.gte(iqsharpVersion["iqsharp"], requiredVersion)) {
                        vscode.window.showInformationMessage(`Currently IQ# version is up to date (${iqsharpVersion["iqsharp"]}).`);
                        return false;
                    }

                    // If we made it here, we need to install IQ#. This can fail if it's already installed.
                    // While dotnet does offer an update command, it's often more reliable to just uninstall and reinstall.
                    // Thus, we uninstall here before proceeding to the install step below.
                    return promisify(cp.exec)(
                        `"${dotNetSdk.path}" tool uninstall --global Microsoft.Quantum.IQSharp`
                    )
                    .then(() => true);
                }
                return true;
            }
        )
        .then(
            needToInstall => {
                if (needToInstall) {
                    let versionSpec =
                        requiredVersion === undefined
                        ? ""
                        : `::${requiredVersion}`;
                    return promisify(cp.exec)(
                        `"${dotNetSdk.path}" tool install --global Microsoft.Quantum.IQSharp${versionSpec}`
                    )
                    .then(() => {
                        // Check what version actually got installed and report that back.
                        findIQSharpVersion()
                            .then(installedVersion => {
                                if (installedVersion === undefined) {
                                    throw new Error("Could not detect IQ# version after installing.");
                                }
                                if (installedVersion["iqsharp"] === undefined) {
                                    throw new Error("Newly installed IQ# did not report a version.");
                                }
                                vscode.window.showInformationMessage(`Successfully installed IQ# version ${installedVersion["iqsharp"]}`);
                            });
                    });
                }
            }
        )
        .catch(
            reason => {
                vscode.window.showWarningMessage(`Could not install IQ#:\n${reason}`);
            }
        );
}
