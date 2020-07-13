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

        // TODO:
        // The default value of `this.templatePath` points to the local AppData roaming data of
        // VS Code when running in this context. This needs to be updated to use the install location
        // from the appropriate extension context.
    }

    prompting() {
        let done = this.async();

        // This dictionary maps the public description of the project type to the name
        // of the folder with the corresponding template files.
        const projectTypes: {[key: string]: string} = {
            "Standalone console application": "application",
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

        console.log(this.templatePath());

        let sourceDir = path.join(this.templatePath(), this.options.projectType);
        let targetDir = this.options.outputUri.fsPath;
        fs.mkdir(targetDir);

        console.log("ASync read has started.");

        // TODO: Determine right namespace name to use.
        let namespaceName = "UserNamespace";

        fs.readdir(sourceDir, (err, files) => {
            if (err){
                throw err;
            }
            files.forEach( (filename) => {
                console.log(filename);
                console.log(path.join(sourceDir, filename));
                console.log(path.join(targetDir, filename));

                let output = this.fs.copyTpl(
                    path.join(sourceDir, filename),
                    path.join(targetDir, filename),
                    {  name: namespaceName }
                );

            console.log(output);
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


