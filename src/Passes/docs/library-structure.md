# Library structure for passes

An important part of this PR is that it proposes a structure for passes: It is suggested that each pass has their own subcode base. The reason for this proposal is that it makes it very easy to add and remove passes as well as decide which passes to link against. Each pass is kept in its own subdirectory under `libs`:

```
libs
├── CMakeLists.txt
└── OpsCounter
    ├── OpsCounter.cpp
    └── OpsCounter.hpp
```

Adding a new pass is easy using the `manage` tool developed in this PR:

```
./manage create-pass HelloWorld
Available templates:

1. Function Pass

Select a template:1
```

which results in a new pass code in the `libs`:

```
libs
├── CMakeLists.txt
├── HelloWorld
│   ├── HelloWorld.cpp
│   ├── HelloWorld.hpp
│   └── SPECIFICATION.md
└── OpsCounter
    ├── OpsCounter.cpp
    └── OpsCounter.hpp
```

A full example of how to create a basic function pass is included in the README.md file for anyone interested.
