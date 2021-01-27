# Contributing to Q#

Welcome, and thank you for your interest in contributing to Q# and the Quantum Development Kit!

There are many ways in which you can contribute. The goal of this document is to provide a high-level overview of how you can get involved. For more details on how to contribute to Q# or the rest of the Quantum Development Kit, please see the [contribution guide](https://docs.microsoft.com/quantum/contributing/).

## Asking and Answering Questions 

Have a question? The `q#` tags on [Stack Overflow](https://stackoverflow.com/questions/tagged/q%23) and [Quantum Computing StackExchange](https://quantumcomputing.stackexchange.com/questions/tagged/q%23) are great places to ask questions about Q#, Quantum Development Kit and quantum computing in general.
You can learn more about our work on the [Q# Development Blog](https://devblogs.microsoft.com/qsharp/) and ask questions in the comments as well.
Your question will server as resource to others searching for help. 

Or maybe you have figured out how that hard-to-understand concept works? Share your knowledge! 
If you are interested in contributing to the documentation around Q# and its libraries, please see the [MicrosoftDocs/quantum-docs-pr](https://github.com/MicrosoftDocs/quantum-docs-pr) repository.

## Reporting Issues

Have you identified a reproducible problem in the Quantum Development Kit?
Have a feature request?
We want to hear about it!

The Quantum Development Kit is distributed across multiple repositories. Filing an issue against the correct repository will help address it swiftly. Check the list [in the contribution guide](https://docs.microsoft.com/quantum/contributing/#where-do-contributions-go) to figure out which repo is a good place to file it.
You can follow the [template](https://github.com/microsoft/qsharp-compiler/issues/new?assignees=&labels=bug&template=bug_report.md&title=) for reporting issues on this repository. 

## Contributing Code

If you are interested in helping fix issues you or someone else encountered, 
please make sure that the corresponding issue has been filed on the repository. 
Check that nobody is currently working on it and that it has indeed been marked as bug. 
If that's the case, indicate on the issue that you are working on it, 
and link to the corresponding GitHub page where the fix is being developed. 
If someone is already working on a fix, ask if you can help or see what other things can be done.
If an issue is labeled as feature, please follow the guidelines related to contributing features. 
Sometimes it may take a couple of days for us to label issues appropriately. 
If an issue has not been labeled yet, please indicate that you would like to work on it and be patient - 
we are a small team and are doing our best to be quick with responding to your inquiry!

**Note:**
Issues related to the design and evolution of the Q# language are marked with 
[Area-Language](https://github.com/microsoft/qsharp-compiler/issues?q=is%3Aissue+is%3Aopen+label%3AArea-Language) 
and are only temporarily tracked on this repository and won't be labeled as bug or feature. 

If you are interested in contributing a new feature, 
please first check if a similar functionality has already been requested. 
If so, consider contributing to the discussion around it rather than filing a separate issue.
If no open or closed issue with such a request already exists, 
please file one following the [feature request template](https://github.com/microsoft/qsharp-compiler/issues/new?assignees=&labels=feature&template=feature_request.md&title=). 
We will respond to feature requests and follow up with a discussion around its feasibility, 
how one might go about implementing it, and whether that is something we would consider adding to our repo. 
There are several reasons why we might not be able to eventually merge even a great feature for one reason or another. 
Take a look at our general contribution guide for [reasons why this might be the case](https://docs.microsoft.com/quantum/contributing/code#when-well-reject-a-pull-request). 
Even if we are not able to incorporate something into the packages and extensions we distribute, 
we encourage you to pursue your passion project in your own fork, 
and share and discuss your thoughts and progress on the corresponding issue. 

If you are looking for a place to get started with contributing code, 
search for example for the [good-first-issue](https://github.com/microsoft/qsharp-compiler/labels/good%20first%20issue) or [help-wanted](https://github.com/microsoft/qsharp-compiler/labels/help%20wanted) labels. 
Also, look for issues that have already been discussed in more detail, 
and check if you can help someone who has already started working on it. 

Whether you want to help fixing bugs or add new features, please take a look at our general guide for [Contributing Code](https://docs.microsoft.com/quantum/contributing/code).

## Formatting Code

Contributions should follow the code formatting guidelines for this repository.

### C#

[StyleCop](https://github.com/DotNetAnalyzers/StyleCopAnalyzers) is configured for every C# project in this repository.
Your IDE should warn you if you write code that does not follow the style guide.

### F#

F# code is automatically formatted using [Fantomas](https://github.com/fsprojects/fantomas).
To install the correct version of Fantomas for this repository, run `dotnet tool restore`.
To format a specific file, run `dotnet tool run fantomas MyFile.fs`.
To format every file in a folder, run `dotnet tool run fantomas -r MyFolder`.

You can also configure your editor to run Fantomas on the current file.
For example, in Visual Studio, open Tools - External Tools.
Click Add, and fill in the fields with:

<dl>
  <dt>Title</dt>
  <dd>Fantomas</dd>

  <dt>Command</dt>
  <dd><code>dotnet.exe</code></dd>

  <dt>Arguments</dt>
  <dd><code>tool run fantomas $(ItemPath)</code></dd>

  <dt>Initial directory</dt>
  <dd><code>$(SolutionDir)</code></dd>
</dl>

You can now run Fantomas on the current file by clicking Tools - Fantomas.

---

And last but not least:

# Thank You!

Your contributions to open source, large or small, make great projects like this possible.
Thank you for taking the time to contribute.
