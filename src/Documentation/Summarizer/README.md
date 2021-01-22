# summarize_documentation

This utility summarizes Markdown documentation gathered from one or more compilation units,
producing namespace and TOC files from the gathered documentation.

To use:

```bash
$ dotnet tool restore
$ dotnet script summarize-documentation.csx obj/qsharp/docs/*.md obj/qsharp/docs
```
