// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Quantum.Sdk.BuildTasks
{
    public class SortByPriority : Task
    {
        // The [Required] attribute indicates a required property.
        // If a project file invokes this task without passing a value
        // to this property, the build will fail immediately.
        [Required]
        public string MyProperty { get; set; }

        public override bool Execute()
        {
            // Log a high-importance comment
            Log.LogMessage(MessageImportance.High,
                "The task was passed \"" + MyProperty + "\".");
            return true;
        }
    }
}
