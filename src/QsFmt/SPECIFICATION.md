# Q# Formatter Design Specification Document

QsFmt is a source code formatter for Q#.

## Where to get the formatter

???

## How to use the command line formatter

You can use the command-line tool by running `dotnet run -p App` from this folder.
This will only print the formatted code to the console, and won't overwrite your files, so it's safe to use.

## How errors are handled

Parsing errors may occur if the formatter is given text that is not proper Q# code. (What will happen?)
Errors may occur during a transformation of the CST (what is this, some kind of tree?) if a transformation encounters an issue. (What will happen?)

## Rules that are run

Collapsed Spaces - removes duplicate spaces so that there is only a single space where spaces are used
Operator Spacing - ensure that operators have the appropriate spaces in them
New Lines - removes excessive new lines
Indentation - ensures that there are appropriate indentation for the text
