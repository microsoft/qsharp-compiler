// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import yo = require("yeoman-generator");
import yosay = require("yosay");

export default class QSharpGenerator extends yo {
  prompting() {
    // Have Yeoman greet the user.
    this.log(
      yosay("Welcome to the spectacular Q# generator!")
    );

    const prompts = [
      {
        type: "confirm",
        name: "someAnswer",
        message: "Would you like to enable this option?",
        default: true
      }
    ];

    return this.prompt(prompts).then(props => {
      // To access props later use this.props.someAnswer;
      // TODO: Store the properties in QSharpGenerator object
    });
  }

  writing() {
    this.fs.copy(
      this.templatePath("dummyfile.txt"),
      this.destinationPath("dummyfile.txt")
    );
  }

  install() {
    this.installDependencies();
  }
}

