# Contributing

This document sets out some light requirements for contributing to QAT. In case you do not feel like reading this style guide, just run

```sh
./manage runci
```

from the `src/Passes` directory before making a pull request. This script enforces all requirements described below programmatically. You can then refer to this guide for an explanation for why and how.

## Why do we need a style guide?

Consistency and readability such that it is easy to read and understand code that was not written by yourself. For example, if one developer uses `CamelCase` for namespaces and `snake_case` for classes while another uses `snake_case` for namespaces and `CamelCase` you may end up with code sections that looks like this

```cpp
int32_t main()
{
  name_space1::Class1 hello;
  NameSpace2::class_name world;
}
```

which is hard to read.

## What does the style guide apply to?

The style guide applies to any new code written as well as code that is being refactored added to the `Passes` library. We do not rewrite existing code for the sake just changing the style.

## Style discrepancy

In case of a discrepancy between this guideline and `clang-tidy` or `clang-format`,
clang tools rule. In case of discrepancy between this guide and any guides subsequently referenced guides, this guide rules. However, feel free to suggest changes. Changes will be incorporated on the basis
that updated styles are apply to new code and not existing code.

## Naming

Naming is taken from the [Microsoft AirSim](https://github.com/microsoft/AirSim/blob/master/docs/coding_guidelines.md) project.

| **Code Element**         | **Style**                        | **Comment**                                                                                                                                   |
| ------------------------ | -------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Namespace                | snake_case                       | Differentiates `namespace::ClassName` and `ClassName::SubClass` names                                                                         |
| Class name               | CamelCase                        | To differentiate from STL types which ISO recommends (do not use "C" or "T" prefixes)                                                         |
| Type aliases             | CamelCase                        | To differentiate from STL types which ISO recommends                                                                                          |
| Template type parameters | CamelCase                        | To differentiate from STL types which ISO recommends                                                                                          |
| Function name            | camelCase                        | Lower case start is almost universal except for .NET world                                                                                    |
| Parameters/Locals        | snake_case                       | Vast majority of standards recommends this because \_ is more readable to C++ crowd (although not much to Java/.NET crowd)                    |
| Member variables         | snake_case_with\_                | The prefix \_ is heavily discouraged as ISO has rules around reserving \_identifiers, so we recommend suffix instead                          |
| Enums and its members    | CamelCase                        | Most except very old standards agree with this one                                                                                            |
| Globals                  | g_snake_case                     | Avoid using globals whenever possible, but if you have to use `g_`.                                                                           |
| Constants                | UPPER_CASE                       | Very contentious and we just have to pick one here, unless if is a private constant in class or method, then use naming for Members or Locals |
| File names               | Match case of class name in file | Lot of pro and cons either way but this removes inconsistency in auto generated code (important for ROS)                                      |

## Modernise when possible

In general, modernise the code where possible. For instance, prefer `using` over `typedef`.

## Header guards

Prefer `#pragma once` over `#ifdef` protection.

## Code TODOs must contain owner name or Github issue

```sh
./manage runci
(...)
Passes/src/OpsCounter/OpsCounter.cpp:39:21: error: missing username/bug in TODO [google-readability-todo,-warnings-as-errors]
                    // TODO: Fails to load if this is present
                    ^~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                    // TODO(tfr): Fails to load if this is present
```

## Always add copyrights

Always add copyrights at the top of the file.

```text
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
```

For header files, prefer to put `#prama once` before the copyright.

## Tabs vs. spaces

We indent with 2 spaces. Nested namespaces are not indented.
