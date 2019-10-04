/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

namespace Microsoft.Quantum.QsProjectSystem
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the package that is required e.g. to define custom commands (ctmenu).
    /// </summary>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Description("A custom project type based on CPS")]
    [Guid(VsPackage.PackageGuid)]
    public sealed class VsPackage : Package
    {
        public const string PackageGuid = "456e79ed-aeb7-4330-b5e8-ce59c10488b1";

        /// <summary>
        /// The GUID for this project type, also appears under the VS registry hive's Projects key.
        /// </summary>
        public const string ProjectTypeGuid = "18755719-ded1-4d14-a08b-eb704847195e";

        /// <summary>
        /// The file extension of this project type (without preceding period).
        /// </summary>
        public const string ProjectExtension = "qsproj";
    }
}
