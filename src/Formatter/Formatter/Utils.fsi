// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// General utility functions.
module internal Microsoft.Quantum.QsFmt.Formatter.Utils

/// Curries a function of two arguments.
val curry: ('a * 'b -> 'c) -> 'a -> 'b -> 'c
