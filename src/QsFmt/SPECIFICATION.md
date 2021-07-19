# Q# Formatter Design Specification Document

QsFmt is a source code formatter for Q#.

## Where to get the formatter

The <package-name> NuGet package will be distributed with the rest of Q# on NuGet.org.
As a dotnet tool, it is installed with the command `dotnet tool install <package-name>`.

## How to use the command line formatter

After installing the tool the command `qsfmt` is used to run the formatter.
You can use the command-line tool by running `dotnet run -p App` from this folder.
There are two commands supported for the tool: 'format' and 'update'.
The 'format' command runs the tool as a formatter for Q# code, running only those transformation rules that update whitespace and affect indentation. The underlying code will not be changed.
The 'update' command allows the tool to be used as a means of updating old Q# code, replacing deprecated syntax with newer supported syntax. These transformation rules change the actual code given to them, and may occasionally fail.
These two commands work separately and can't be run together. If the user wishes to both format and update their code, they would need to run the tool twice, first with 'update' and then with 'format'.
The output of the format is printed to the console; it won't overwrite the source file given to it. This output may be redirected to a file through the usual console methods.

The tool will initially only support the 'update' command, as the formatting functionality is still being worked on. The 'format' command will be unavailable until it is supported.

## Rules that are run

The tool will parse the input into a concrete syntax tree using ANTLR. This tree will then have rules applied to it in the form of transformations on the tree.
The transformations are separated into two groups: transformations run by the 'format' command, and transformations run by the 'update' command.
Listed here are some of the transformations we intend to use.

### Formatting Transformations

Collapsed Spaces - removes duplicate spaces so that there is only a single space where spaces are used
Operator Spacing - ensure that operators have the appropriate spaces in them
New Lines - removes excessive new lines
Indentation - ensures that there are appropriate indentation for the text

### Updating Transformations

Updating Transformations remove outdated syntax that is deprecated and will be no longer supported in the future.

Array Syntax - [proposal](https://github.com/microsoft/qsharp-language/blob/main/Approved/2-enhanced-array-literals.md)
Using Syntax - [proposal](https://github.com/microsoft/qsharp-language/blob/main/Approved/1-implicitly-scoped-qubit-allocation.md)

## How errors are handled

If an exception is thrown, that will be surfaced to the user and is a bug.

Parsing errors may occur if the formatter is given text that is not proper Q# code. In this case ANTLR gives the appropriate error message.
Errors should not occur during a formatting transformation. All formatting transformations should never encounter the syntax they are expected to change and be unable to perform the change.
Errors may occur during an updating transformation of the concrete syntax tree if an updating transformation encounters an issue, such as syntax that it would be expected to updated, but can't for some reason.

Errors of all kinds should be collected and reported to the user after all transformations are finished.
 - How? To the console? That is where we are putting the regular output (the formatted/updated code).
