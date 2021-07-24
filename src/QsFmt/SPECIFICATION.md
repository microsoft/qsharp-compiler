# Q# Formatter Design Specification Document

QsFmt is a source code formatter for Q#.

## Where to get the formatter

The `Microsoft.Quantum.QSharp.Formatter` NuGet package will be distributed with the rest of Q# on NuGet.org.
As a dotnet tool, it is installed with the command `dotnet tool install Microsoft.Quantum.QSharp.Formatter`.

## How to use the command line formatter

After installing the tool the command `qsfmt` is used to run the formatter.
There are two commands supported for the tool: 'format' and 'update'.
The 'format' command runs the tool as a formatter for Q# code, running only those transformation rules that update whitespace and affect indentation. The underlying code will not be changed.
The 'update' command allows the tool to be used as a means of updating old Q# code, replacing deprecated syntax with newer supported syntax. These transformation rules change the actual code given to them, and may occasionally fail.
These two commands work separately and can't be run together. If the user wishes to both format and update their code, they would need to run the tool twice, first with 'update' and then with 'format'.
Both of these commands should be idempotent, meaning that iterated uses of the commands will not continue to have effects on the inputs.

The tool will initially only support the 'update' command, as the formatting functionality is still being worked on. The 'format' command will be unavailable until it is supported.

## Input and Output
Input files to the formatter will be the last argument(s) to the tool.
At least one input argument is expected. If no inputs are given, usage information is printed.
Inputs can be given as one to many paths to directories, files, or any combination. In directories specified, all files with the '.qs' extension found will be taken as input.
The `--recurse` option can be specified to process the input directories recursively for input files. You can specify this for all input directories, not per-directory.

(Look into pattern matching and wild cards)

The output of the formatter is to overwrite the input files that it processes.

(Look into specifying backups. Flag for turning it on and use '.bak' extension.)

## Rules that are run

The tool will parse the input into a concrete syntax tree using ANTLR. This tree will then have rules applied to it in the form of transformations on the tree.
The transformations are separated into two groups: transformations run by the 'format' command, and transformations run by the 'update' command.
Listed here are some of the transformations we intend to use.

### Formatting Transformations

 - Collapsed Spaces - removes duplicate spaces so that there is only a single space where spaces are used
 - Operator Spacing - ensure that operators have the appropriate spaces in them
 - New Lines - removes excessive new lines
 - Indentation - ensures that there are appropriate indentation for the text

### Updating Transformations

Updating transformations remove outdated syntax that is deprecated and will be no longer supported in the future.

 - Array Syntax - [proposal](https://github.com/microsoft/qsharp-language/blob/main/Approved/2-enhanced-array-literals.md)
 - Using Syntax - [proposal](https://github.com/microsoft/qsharp-language/blob/main/Approved/1-implicitly-scoped-qubit-allocation.md)
 (Colon syntax for characteristics)
 (Old unit syntax)
 (Look at compiler deprecation warnings)

## How errors are handled

If an internal unhandled exception is thrown by the tool, that will be surfaced to the user and is a bug with the tool that should be filed and addressed.
Exceptions like these will prevent any output from being written on the file that caused the exception. The tool will try to recover from the exception to process other input files.

Parsing errors may occur if the formatter is given text that is not proper Q# code. In this case ANTLR gives the appropriate error message.
Parsing errors prevent the given input file from being processed. Other input files may still be processed.
Errors should not occur during a formatting transformation. All formatting transformations should never encounter the syntax that they are expected to change but are unable to change.
Warnings may occur during an updating transformation of the concrete syntax tree if an updating transformation encounters an issue, such as syntax that it would be expected to update, but can't for some reason.
Warnings like this will prevent the referenced code piece from being updated, but the rest of the file will still be processed.

(Warnings should contain file and line number.)

Errors and warnings of all kinds should be collected and reported to the user through stderr.
Errors and warnings should be handled in a way so as not to affect the tool's idempotency.
