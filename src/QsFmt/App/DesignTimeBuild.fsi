// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsFmt.App.DesignTimeBuild

/// Given a path to a project file, returns the list of files associated
/// to the project and the Quantum SDK version.
/// Errors if given a file that is not a project file using Microsoft.Quantum.Sdk.
val getSourceFiles: string -> string list * string

/// Initializes the assembly load context needed for building project files.
val assemblyLoadContextSetup: Unit -> Unit
