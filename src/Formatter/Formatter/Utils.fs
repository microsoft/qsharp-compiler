/// General utility functions.
module internal QsFmt.Formatter.Utils

/// Curries a function of two arguments.
let curry f x y = f (x, y)
