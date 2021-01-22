// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#r "nuget: YamlDotNet, 9.1.4"
#r "nuget: System.CommandLine, 2.0.0-beta1.20574.7"
#r "nuget: Microsoft.Extensions.FileSystemGlobbing, 5.0.0"
#nullable enable

using YamlDotNet;
using System.CommandLine;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Extensions.FileSystemGlobbing;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public record NamespaceItem(
    string Summary,
    string Name,
    string Namespace,
    string Uid,
    string Kind
);

public record Namespace()
{
    public string? Summary { get; init; } = null;
    public string? Uid { get; init; } = null;
    public string? Name { get; init; } = null;
    public IImmutableSet<NamespaceItem> Items = ImmutableHashSet<NamespaceItem>.Empty;

    public IEnumerable<Dictionary<string, string>> ItemsOfKind(string kind) =>
        Items
            .Where(item => item.Kind == kind)
            .OrderBy(item => item.Uid)
            .Select(item =>
                new Dictionary<string, string>
                {
                    ["uid"] = item.Uid,
                    ["summary"] = item.Summary
                }
            );
}

public static IEnumerable<string> Glob(this string glob)
{
    // First check if the path is absolute or relative, and determine
    // the base directory accordingly.
    var (baseDir, relativeGlob) =
        Path.IsPathRooted(glob)
        ? (Path.GetDirectoryName(glob), Path.GetFileName(glob))
        : (Directory.GetCurrentDirectory(), glob);
    return new Matcher().AddInclude(relativeGlob).GetResultsInFullPath(baseDir);
}

public static TValue GetValueOrNew<TKey, TValue>(
        this IDictionary<TKey, TValue> dict,
        TKey key
)
where TValue: new()
where TKey: notnull =>
    dict.TryGetValue(key, out var value)
    ? value
    : new TValue();

static ISerializer serializer = new SerializerBuilder().Build();
static IDeserializer deserializer = new DeserializerBuilder().Build();

public Dictionary<string, object>? ParseFrontMatter(string source)
{
    using var file = File.OpenRead(source);
    using var input = new StreamReader(file);
    var parser = new Parser(input);
    parser.Consume<StreamStart>();
    parser.Consume<DocumentStart>();
    Dictionary<string, object>? frontMatter = null;
    try
    {
        frontMatter = deserializer.Deserialize<Dictionary<string, object>>(parser);
    }
    catch { return null; }

    if (!parser.TryConsume<DocumentEnd>(out _))
    {
        // No front matter to return.
        return null;
    }

    return frontMatter;
}

public static ImmutableDictionary<string, Namespace> AddFrontMatter(this ImmutableDictionary<string, Namespace> namespaces, Dictionary<string, object> frontMatter)
{
    var name = (string)frontMatter["qsharp.name"];
    namespaces =
        frontMatter["qsharp.kind"] as string == "namespace"
        // If this is a namespace, add its front matter to whatever
        // we may already know about that namespace.
        ? namespaces.SetItem(
                name,
                namespaces.GetValueOrNew(name) with
                {
                    Summary = (string)frontMatter["qsharp.summary"],
                    Name = (string)frontMatter["qsharp.name"],
                    Uid = (string)frontMatter["uid"]
                }
            )
        // If this is a namespace item, add it to the items for that
        // namespace.
        : frontMatter.TryGetValue("qsharp.namespace", out var ns)
            ? namespaces.SetItem(
                (string)ns,
                namespaces.GetValueOrNew((string)ns) with
                {
                    Items = namespaces.GetValueOrNew((string)ns).Items.Add(
                        new NamespaceItem(
                            Summary: (string)frontMatter["qsharp.summary"],
                            Name: name,
                            Namespace: (string)ns,
                            Uid: (string)frontMatter["uid"],
                            Kind: (string)frontMatter["qsharp.kind"]
                        )
                    )
                }
            )
            : throw new Exception($"Expected a qsharp.namespace in header.");
    return namespaces;
}

public static void SerializeToFile(this object obj, string path)
{
    using var file = File.OpenWrite(path);
    using var writer = new StreamWriter(file);
    serializer.Serialize(writer, obj);
}

public void Summarize(string sources, string outputPath)
{
    var namespaces = ImmutableDictionary<string, Namespace>.Empty;
    foreach (var source in sources.Glob())
    {
        Console.WriteLine($"Summarizing {source}...");
        var frontMatter = ParseFrontMatter(source);
        if (frontMatter == null)
        {
            Console.WriteLine($"    [WARNING] No front matter found for {source}.");
            continue;
        }

        var name = (string)frontMatter["qsharp.name"];
        try
        {
            namespaces = namespaces.AddFrontMatter(frontMatter);
        }
        catch (Exception e)
        {
            Console.WriteLine($"    [ERROR] Exception adding {source} to summaries:\n\t{e.Message}");
        }
    }

    foreach (var (nsName, ns) in namespaces)
    {
        var uid = ns.Name ?? nsName;
        var name = ns.Name ?? nsName;
        var page = new
        {
            uid = uid,
            name = name,
            summary = ns.Summary,
            operations = ns.ItemsOfKind("operation"),
            functions = ns.ItemsOfKind("function"),
            newtypes = ns.ItemsOfKind("udt")
        };

        var path = Path.Join(outputPath, name.ToLower() + ".yml");
        Console.WriteLine($"Writing summary to {path}...");
        page.SerializeToFile(path);
    }

    var tocPage = namespaces
        .OrderBy(ns => ns.Key)
        .Select(ns => new
        {
            uid = ns.Value.Uid,
            name = ns.Key,
            items = ns.Value.Items
                .OrderBy(item => item.Uid)
                .Select(item => new
                {
                    name = item.Name,
                    uid = item.Uid
                })
                .ToList()
        })
        .ToList();
    var tocPath = Path.Join(outputPath, "toc.yml");
    Console.WriteLine($"Writing TOC to {tocPath}...");
    tocPage.SerializeToFile(tocPath);
}


Summarize(Args[0], Args[1]);
