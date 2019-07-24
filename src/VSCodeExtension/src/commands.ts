'use strict';
import * as vscode from 'vscode';
import * as cp from 'child_process';

import { DotnetInfo } from './dotnet';
import { IPackageInfo } from './packageInfo';

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

export function createNewProject(dotNetSdk: DotnetInfo) {    
    const projectTypes: {[key: string]: string} = {
        "Standalone console application": "console",
        "Quantum library": "classlib",
        "Unit testing project": "xunit"
    };
    let errorMessage = "";
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
            .then(uri => {
                let proc = cp.spawn(
                    dotNetSdk.path,
                    ["new", projectType, "-lang", "Q#", "-o", uri.fsPath]
                );
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
                            vscode.window.showErrorMessage(
                                `.NET Core SDK exited with code ${code} when creating a new project:\n${errorMessage}`
                            );
                        }
                    }
                );
            });
        }
    );
}

export function installTemplates(dotNetSdk: DotnetInfo, packageInfo ?: IPackageInfo) {
    let packageVersion = 
        packageInfo === undefined
        ? ""
        : `::${packageInfo.version}`;
    let errorMessage = "";
    let proc = cp.spawn(
        dotNetSdk.path,
        ["new", "--install", `Microsoft.Quantum.ProjectTemplates${packageVersion}`]
    );
    proc.stderr.on(
        'data', data => {
            errorMessage = errorMessage + data;
        }
    );
    proc.on(
        'exit',
        (code, signal) => {
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
