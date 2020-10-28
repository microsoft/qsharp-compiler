# Remote Development Container for Visual Studio Code (preview) #

This folder defines a _development container_ for the Quantum Development Kit to get up and running with your development environment for contributing to the Q# compiler.

## What is a Development Container? ##

Visual Studio Code allows for using [Docker](https://docs.microsoft.com/dotnet/standard/microservices-architecture/container-docker-introduction/docker-defined) to quickly define development environments, including all the compilers, command-line tools, libraries, and programming platforms you need to get up and running quickly.
Using the definitions provided in this folder, Visual Studio Code can use Docker to automatically install the correct version of the Quantum Development Kit as well as other software you might want to use with Q#, such as Python and Jupyter Notebook --- all into an isolated container that doesn't affect the rest of the software you have on your system.

Next steps:
- [Visual Studio Code: Developing inside a container](https://code.visualstudio.com/docs/remote/containers)

## Getting Started ##

To use this development container, follow the installation instructions on the [Visual Studio Code site](https://code.visualstudio.com/docs/remote/containers#_installation) to prepare your machine to use development containers such as this one.
Once you have done so, clone the [**microsoft/quantum**](https://github.com/microsoft/quantum) repository and open the folder in Visual Studio Code.
You should be prompted to reopen the folder for remote development in the development container; if not, make sure that you have the right extension installed from above.

Once you follow the prompt, Visual Studio Code will automatically configure your development container by installing the Quantum Development Kit into a new image, including installing the .NET Core SDK, project templates, Jupyter Notebook support, and the Python host package.
This process will take a few moments, but once it's complete, you can then open a new shell as normal in Visual Studio Code; this shell will open a command line in your new development container.
The Q# compiler will then be available in the `/workspace/qsharp-compiler/` folder of your development container, so you can easily build different parts of the compiler by using `dotnet build`.
