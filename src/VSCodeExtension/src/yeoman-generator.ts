// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as vscode from 'vscode';
import yo = require("yeoman-generator");
import yosay = require("yosay");

export class QSharpGenerator extends yo {

    constructor(args : any, opts : any) {
        super(args, opts);
        console.log(
            yosay("Welcome to the Q# generator!")
        );
    }

    prompting() {
        let done = this.async();
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
                    this.options.outputFolder = uri;
                    done();
                });
            }
        );
    }

    writing() {
        // TODO: Implement project creation..
        console.log(
            yosay("Project will be created.")
        );
    }
}


