// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
import * as vscode from 'vscode';
import * as cp from 'child_process';

import { DotnetInfo, findIQSharpVersion, DotNetSdk } from './dotnet';
import { IPackageInfo } from './packageInfo';
import * as semver from 'semver';
import { oc } from 'ts-optchain';
import { reporter, EventNames } from './telemetry';

export function registerCommand(context: vscode.ExtensionContext, name: string, action: () => void) {
    context.subscriptions.push(
        vscode.commands.registerCommand(
            name,
            () => {
                reporter.sendTelemetryEvent(
                    EventNames.commandStarted,
                    {
                        "commandName": name
                    }
                );
                let startedAt = Date.now();
                action();
                let elapsedMilliseconds = Date.now() - startedAt;
                reporter.sendTelemetryEvent(
                    EventNames.commandCompleted,
                    {
                        "commandName": name
                    },
                    {
                        "elapsedMilliseconds": elapsedMilliseconds
                    }
                );
            }
        )
    );
}

function createNewProjectAtUri(dotNetSdk: DotnetInfo, projectType: string, uri: vscode.Uri) {
    let proc = cp.spawn(
        dotNetSdk.path,
        ["new", projectType, "-lang", "Q#", "-o", uri.fsPath]
    );
    
    let errorMessage = "";
    proc.stderr.on('data', data => {
        errorMessage = errorMessage + data;
    });

    proc.on(
        'exit', (code, signal) => {
            if (code === 0) {
                const openItem = "Open new project...";
                vscode.window.showInformationMessage(
                    `Successfully created new project at ${uri.fsPath}.`,
                    openItem
                ).then(
                    (item) => {
                        if (item === openItem) {
                            vscode.commands.executeCommand(
                                "vscode.openFolder",
                                uri
                            ).then(
                                (value) => {},
                                (value) => {
                                    vscode.window.showErrorMessage("Could not open new project");
                                }
                            );
                        }
                    }
                );
            } else {
                // Check if the problem was that the project templates are missing.
                // If so, we can give a more helpful error message and offer the
                // user to install templates.
                if (errorMessage.includes("Q#") && errorMessage.includes("-lang")) {
                    const installTemplatesItem = "Install project templates and retry";
                    vscode.window.showErrorMessage(
                        "Project creation failed. The Q# project templates may not be installed.",
                        installTemplatesItem
                    )
                    .then(
                        item => {
                            if (item === installTemplatesItem) {
                                vscode.commands.executeCommand(
                                    "quantum.installTemplates"
                                ).then(
                                    () => createNewProjectAtUri(dotNetSdk, projectType, uri)
                                );
                            }
                        }
                    );
                } else {
                    vscode.window.showErrorMessage(
                        `.NET Core SDK exited with code ${code} when creating a new project:\n${errorMessage}`
                    );
                }
            }
        }
    );
}

export function createNewProject(dotNetSdk: DotnetInfo) {    
    const projectTypes: {[key: string]: string} = {
        "Standalone console application": "console",
        "Quantum library": "classlib",
        "Unit testing project": "xunit"
    };
    vscode.window.showQuickPick(
        Object.keys(projectTypes)
    ).then(
        projectTypeSelection => {
            if (projectTypeSelection === undefined) {
                throw undefined;
            }
            let projectType = projectTypes[projectTypeSelection];

            vscode.window.showSaveDialog({
                saveLabel: "Create Project"
            }).then(
                (uri) => {
                    if (uri !== undefined) {
                        if (uri.scheme !== "file") {
                            vscode.window.showErrorMessage(
                                "New projects must be saved to the filesystem."
                            );
                            throw new Error("URI scheme was not file.");
                        }
                        else {
                            return uri;
                        }
                    } else {
                        throw undefined;
                    }
                }
            )
            .then(uri => createNewProjectAtUri(dotNetSdk, projectType, uri));
        }
    );
}

export function installTemplates(dotNetSdk: DotnetInfo, packageInfo?: IPackageInfo) {
    let packageVersion =
        oc(packageInfo).nugetVersion
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

export function installOrUpdateIQSharp(dotNetSdk: DotNetSdk, requiredVersion?: string) {
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
                    return dotNetSdk.exec("tool uninstall --global Microsoft.Quantum.IQSharp")
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
                    return dotNetSdk.exec(`tool install --global Microsoft.Quantum.IQSharp${versionSpec}`)
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
                                // Now that we've confirmed that the tool is available, use it to
                                // register IQ# with Jupyter.
                                return dotNetSdk.exec("iqsharp install --user")
                                .then(
                                    obj => {
                                        vscode.window.showInformationMessage(`Successfully installed IQ# version ${installedVersion["iqsharp"]}`);
                                        return obj;
                                    }
                                );
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
