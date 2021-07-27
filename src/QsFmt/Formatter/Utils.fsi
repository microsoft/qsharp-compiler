// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/// General utility functions.
module internal Microsoft.Quantum.QsFmt.Formatter.Utils

/// Curries a function of two arguments.
val curry : ('a * 'b -> 'c) -> 'a -> 'b -> 'c

/// Curries a function of three arguments.
val curry3 : ('a * 'b * 'c -> 'd) -> 'a -> 'b -> 'c -> 'd
