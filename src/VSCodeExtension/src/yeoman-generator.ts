// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import yo = require("yeoman-generator");
import yosay = require("yosay");

export class QSharpGenerator extends yo {

    constructor(args : any, opts : any) {
        super(args, opts);
        this.log(
            yosay("Welcome to the Q# generator!")
        );
    }

    prompting() {
        // No need to prompt from the generator.
    }

    writing() {

        // This is a stub only.
        this.log(
            yosay("Writing.")
        );
    }
}


