// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

module Microsoft.Quantum.QsFmt.Formatter.Utils

let curry f x y = f (x, y)

let curry3 f x y z = f (x, y, z)
