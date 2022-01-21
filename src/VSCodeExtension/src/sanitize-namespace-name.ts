// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

export function sanitizeNamespaceName(projectName: string): string {
    // Replace non-alphanumeric characters, except for dots, with underscores. 
    let namespaceName = projectName.replace(/[^A-Za-z0-9_.]/g, "_");

    // Replace illegal character combinations in the namespace name.
    namespaceName = namespaceName.replace(/_\./g, ".");
    namespaceName = namespaceName.replace(/\.+/g, ".");
    namespaceName = namespaceName.replace(/_+/g, "_");
    if (namespaceName.endsWith("_")) {
        namespaceName = namespaceName.substring(0, namespaceName.length - 1);
    }
    return namespaceName;
}
