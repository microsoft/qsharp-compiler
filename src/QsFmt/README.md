# QsFmt: Q# Formatter

QsFmt is a source code formatter and updater for Q#.
It's in the very early stages of development and is currently experimental.
It will very likely eat your code when it tries to format it!

## Usage

Updates the source code in input files:  
&nbsp;&nbsp;&nbsp;&nbsp;`qsfmt update --inputs Path\To\My\File1.qs Path\To\My\File2.qs`  
Updates the source code in project:  
&nbsp;&nbsp;&nbsp;&nbsp;`qsfmt update --project Path\To\My\Project.csproj`

Command Line Options:  
&nbsp;&nbsp;&nbsp;&nbsp;`-i`, `--inputs`: Required. Files or folders to update.  
&nbsp;&nbsp;&nbsp;&nbsp;`-p`, `--project`: Required. The project file for the project to update.  
&nbsp;&nbsp;&nbsp;&nbsp;`-b`, `--backup`: Option to create backup files of input files.  
&nbsp;&nbsp;&nbsp;&nbsp;`-r`, `--recurse`: Option to process input folders recursively.  
&nbsp;&nbsp;&nbsp;&nbsp;`--qsharp-version`: Option to provide a Q# version to the tool.  
&nbsp;&nbsp;&nbsp;&nbsp;`--help`: Display this help screen.  
&nbsp;&nbsp;&nbsp;&nbsp;`--version`: Display version information.

Either the `--inputs` option or the `--project` must be used to specify the input files to the tool.  
The `--recurse` and `--qsharp-version` options can only be used with the `--inputs` option.

## Input and Output
Input to the formatter can be specified in one of two ways.  
Individual files or folders can be specified with the `--input` command-line argument.
Multiple files and folders can be specified after the argument, but at least one is expected.
.qs extension directly under the folder will be processed. If the `--recurse` option is
specified, subfolders will be processed recursively for Q# files.  
The other method of providing input to the formatter is by specifying a Q# project file
with the `--project` command-line argument. When this method is used, exactly one project file
is expected after the argument, and the tool will use MSBuild to determine all applicable Q# source
files under this project. Source files from referenced libraries and other projects will not be processed.
It is worth noting that the tool used to support input directly from the command-line. That is no
longer supported. If there is interest in supporting that method of input, a third input command-line
option specific to that input method may be created in the future.

The output of the formatter is to overwrite the input files that it processes. The `--backup`
option can be specified to create backup files for all input files with the original content in them.

## Rules that are run

Updating rules remove outdated syntax that is deprecated and will be no longer supported in the future.

Updating rules currently in use:
 - Array Syntax - [proposal](https://github.com/microsoft/qsharp-language/blob/main/Approved/2-enhanced-array-literals.md)
 - Using and Borrowing Syntax - [proposal](https://github.com/microsoft/qsharp-language/blob/main/Approved/1-implicitly-scoped-qubit-allocation.md)
 - Parentheses in For Loop Syntax - Removes deprecated parentheses around for-loop range expressions.
 - Unit Syntax - Replaces deprecated unit syntax `()` for `Unit`.

## Update vs. Format

Currently the formatter only supports rules that update deprecated syntax
through the use of the `update` command. A `format` command is being worked on
but is not yet ready for release. This command, when ready, will format Q#
code by running such rules like:
 - Collapsed Spaces - removes duplicate spaces so that there is only a single space where spaces are used
 - Operator Spacing - ensure that operators have the appropriate spaces in them
 - New Lines - removes excessive new lines
 - Indentation - ensures that there are appropriate indentation for the text

## Limitations

- Currently the formatter only supports rules that update deprecated syntax through
  the use of the `update` command. The `format` command is not yet ready for release.
- Many types of syntax are not yet supported and will remain unchanged by the formatter.
- There are several bugs related to other types of syntax, especially callable declaration syntax like
  attributes, type parameters, and specializations. These may be swallowed by the formatter,
  resulting in incorrect output.

## Design

QsFmt uses a [concrete syntax tree](https://en.wikipedia.org/wiki/Parse_tree), which is lossless: a
Q# source file parsed into a CST can be converted back into a string without any loss of information.
QsFmt's syntax tree is modeled on the Q# compiler's abstract syntax tree, but with additional
information on every node for tokens like semicolons and curly braces that are unnecessary in an AST.
Whitespace and comment tokens, known as *trivia tokens*, are attached as a prefix to a non-trivia token.
For example, in the expression `x + y`, `x` has prefix `""`, `+` has prefix `" "`, and `y` has
prefix `" "`.

QsFmt uses [ANTLR](https://www.antlr.org/) to parse Q# programs.
It uses a grammar based on the [grammar in the Q# language specification](https://github.com/microsoft/qsharp-language/tree/main/Specifications/Language/5_Grammar).
ANTLR's parse tree is then converted into Q# Formatter's concrete syntax tree.

Transformation rules are a mapping from one CST to another CST.
Then the transformation pipeline is:

1. Parse a Q# source file into a CST.
2. Apply transformation rules to the CST in order.
3. Unparse the CST into a Q# source file.

This allows transformations to be written separately, and in many cases may be independent from each other.
(However, there may be dependencies where one transformation must run before another.)
This will hopefully make the transformation rules more modular and simpler to write.
