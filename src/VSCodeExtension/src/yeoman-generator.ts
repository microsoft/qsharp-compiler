// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as vscode from 'vscode';
import * as fs from 'fs-extra';
import * as path from 'path';
import yo = require("yeoman-generator");
import yosay = require("yosay");

export class QSharpGenerator extends yo {

    constructor(args : any, opts : any) {
        super(args, opts);
        console.log(
            yosay("Welcome to the Q# generator!")
        );

        this.sourceRoot(path.join(this.options.extensionPath, "templates"));
    }

    prompting() {
        let done = this.async();

        // This dictionary maps the public description of the project type to the name
        // of the folder with the corresponding template files.
        const projectTypes: {[key: string]: string} = {
            "Standalone console application": "application",
            "Quantum application targeted to Honeywell backend": "honeywell",
            "Quantum application targeted to IonQ backend": "ionq",
            "Quantum library": "library",
            "Unit testing project": "unittest"
        };

        vscode.window.showQuickPick(
            Object.keys(projectTypes)
        ).then(
            projectTypeSelection => {
                if (projectTypeSelection === undefined) {
                    throw undefined;
                }
                
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
                    this.options.projectType = projectTypes[projectTypeSelection];
                    this.options.outputUri = uri;
                    done();
                });
            }
        );
    }

    writing() {
        console.log(
            yosay("Creating Q# project.")
        );

        let sourceDir = path.join(this.templatePath(), this.options.projectType);
        let targetDir = this.options.outputUri.fsPath;
        fs.mkdir(targetDir);

        // Namespace is the directory name itself.
        let dirs = targetDir.split(path.sep);

        // In case there is a trailing separator.
        let namespaceName = dirs.pop() || dirs.pop();

        fs.readdir(sourceDir, (err, files) => {
            if (err){
                throw err;
            }
            files.forEach( (filename) => {
                let destinationName = filename;
                let fileExtension = filename.split(".").pop();

                if (fileExtension && fileExtension.toLowerCase() === "csproj") {
                    destinationName = namespaceName + ".csproj";
                }

                this.fs.copyTpl(
                    path.join(sourceDir, filename),
                    path.join(targetDir, destinationName),
                    {  name: namespaceName }
                );
            });
        });

        const openItem = "Open new project...";
        vscode.window.showInformationMessage(
            `Successfully created new project at ${targetDir}.`,
            openItem
        ).then(
            (item) => {
                if (item === openItem) {
                    vscode.commands.executeCommand(
                        "vscode.openFolder",
                        this.options.outputUri
                    ).then(
                        (value) => { },
                        (value) => {
                            vscode.window.showErrorMessage("Could not open new project");
                        }
                    );
                }
            }
        );
    }
}
