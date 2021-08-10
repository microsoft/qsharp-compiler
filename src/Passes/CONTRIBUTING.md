# Contributing (Proposal - WiP)

This document is work in progress and nothing is set in stone. In case you do not want to feel like reading this style guide, just run

```sh
./manage runci
```

from the `Passes` directory as all points defined in this document is automatically enforces. You can then refer to this guide for an explanation for why and how.

## Why do we need a style guide?

Consistency and readibility such that it is easy to read and understand code that was not written by yourself. For example, if one developer uses `CamelCase` for namespaces and `snake_case` for classes while another uses `snake_case` for namespaces and `CamelCase` you may end up with code sections that looks like this

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

## Style discrepency

In case of a discrepency between this guideline and `clang-tidy` or `clang-format`,
clang tools rule. In case of discrency between this guide and any guides subsequently referenced guides, this guide rule. However, feel free to suggest changes. Changes will be incorporated on the basis
that updated styles are apply to new code and not existing code.

## Naming

Naming is taken from the [Microsoft AirSim](https://github.com/microsoft/AirSim/blob/master/docs/coding_guidelines.md) project.

| **Code Element**      | **Style**                        | **Comment**                                                                                                                                   |
| --------------------- | -------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| Namespace             | snake_case                       | Differentiates `namespace::ClassName` and `ClassName::SubClass` names                                                                         |
| Class name            | CamelCase                        | To differentiate from STL types which ISO recommends (do not use "C" or "T" prefixes)                                                         |
| Function name         | camelCase                        | Lower case start is almost universal except for .Net world                                                                                    |
| Parameters/Locals     | snake_case                       | Vast majority of standards recommends this because \_ is more readable to C++ crowd (although not much to Java/.Net crowd)                    |
| Member variables      | snake_case_with\_                | The prefix \_ is heavily discouraged as ISO has rules around reserving \_identifiers, so we recommend suffix instead                          |
| Enums and its members | CamelCase                        | Most except very old standards agree with this one                                                                                            |
| Globals               | g_snake_case                     | Avoid using globals whenever possible, but if you have to use `g_`.                                                                           |
| Constants             | UPPER_CASE                       | Very contentious and we just have to pick one here, unless if is a private constant in class or method, then use naming for Members or Locals |
| File names            | Match case of class name in file | Lot of pro and cons either way but this removes inconsistency in auto generated code (important for ROS)                                      |

## Modernise when possible

In general, modernise the code where possible. For instance, prefer `using` of `typedef`.

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

Seriously, this should not even be a discussion: It does not matter. If you prefer one over the other feel free to write in whatever style you prefer as long as you use `clang-format` before making a PR. Again, the key here is consistency and readibility.
