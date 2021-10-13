// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module internal Microsoft.Quantum.QsFmt.App.DesignTimeBuild

    /// Given a path to a project file, returns the list of files associated
    /// to the project and the Quantum SDK version if found.
    val getSourceFiles : string -> string list * string option

    /// Initializes the ??? // TODO
    val initiate : Unit -> Unit
