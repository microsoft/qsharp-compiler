// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This unfortunately needs to live in this project
// since the functionality is not available in netstandard2.1.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Microsoft.Quantum.QsCompiler
{
    /// <summary>
    /// Context with some basic handling for loading dependencies.
    /// Each assembly loaded via LoadAssembly is loaded into its own context.
    /// For more details, see https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/assemblyloadcontext.md
    /// </summary>
    public class LoadContext : AssemblyLoadContext
    {
        public readonly string PathToParentAssembly;
        private readonly AssemblyDependencyResolver resolver;
        private readonly HashSet<string> fallbackPaths;

        /// <summary>
        /// Adds the given path(s) to the list of paths the loader will search for a suitable .dll when an assembly could not be loaded.
        /// </summary>
        public void AddToPath(params string[] paths) =>
            this.fallbackPaths.UnionWith(paths);

        /// <summary>
        /// Removes the given path(s) from the list of paths the loader will search for a suitable .dll when an assembly could not be loaded.
        /// </summary>
        public void RemoveFromPath(params string[] paths) =>
            this.fallbackPaths.RemoveWhere(paths.Contains);

        private LoadContext(string parentAssembly)
        {
            this.PathToParentAssembly = parentAssembly;
            this.resolver = new AssemblyDependencyResolver(this.PathToParentAssembly);
            this.fallbackPaths = new HashSet<string>();
            this.Resolving += this.OnResolving;
        }

        /// <inheritdoc/>
        protected override Assembly? Load(AssemblyName name)
        {
            var path = this.resolver.ResolveAssemblyToPath(name);
            return path == null ? null : this.LoadFromAssemblyPath(path);
        }

        /// <inheritdoc/>
        protected override IntPtr LoadUnmanagedDll(string name)
        {
            var path = this.resolver.ResolveUnmanagedDllToPath(name);
            return path == null ? IntPtr.Zero : this.LoadUnmanagedDllFromPath(path);
        }

        /// <summary>
        /// Search all fallback paths for a suitable .dll ignoring all exceptions.
        /// Returns the full path to the dll if a suitable assembly could was found.
        /// </summary>
        private string? ResolveFromFallbackPaths(AssemblyName name)
        {
            bool MatchByName(string file) =>
                Path.GetFileNameWithoutExtension(file)
                .Equals(name.Name, StringComparison.InvariantCultureIgnoreCase);

            var found = new List<string>();
            foreach (var dir in this.fallbackPaths)
            {
                try
                {
                    found.AddRange(Directory.GetFiles(dir, "*.dll", SearchOption.AllDirectories).Where(MatchByName));
                }
                catch
                {
                    continue;
                }
            }
            if (found.Count <= 1 || name.Version == null)
            {
                return found.FirstOrDefault();
            }

            var tempContext = new LoadContext(this.PathToParentAssembly);
            var versions = new List<(string, Version?)>();
            foreach (var file in found)
            {
                try
                {
                    var asm = tempContext.LoadFromAssemblyPath(file);
                    var asmVersion = asm.GetName()?.Version;
                    versions.Add((file, asmVersion));
                    if (name.Version.Equals(asmVersion))
                    {
                        if (tempContext.IsCollectible)
                        {
                            tempContext.Unload();
                        }
                        return file;
                    }
                }
                catch
                {
                    continue;
                }
            }
            if (tempContext.IsCollectible)
            {
                tempContext.Unload();
            }
            var matchesMajor = versions.Where(asm => name.Version.Major == asm.Item2?.Major);
            var matchesMinor = matchesMajor.Where(asm => name.Version.Minor == asm.Item2?.Minor);
            var matchesMajRev = matchesMinor.Where(asm => name.Version.MajorRevision == asm.Item2?.MajorRevision);
            return matchesMajRev.Concat(matchesMinor).Concat(matchesMajor).Select(asm => asm.Item1).FirstOrDefault();
        }

        /// <summary>
        /// Last effort to find a suitable dll for an assembly that could otherwise not be loaded.
        /// Search for a suitable .dll in the specified fallback locations.
        /// Does not load any dependencies.
        /// </summary>
        private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName name)
        {
            var path = this.ResolveFromFallbackPaths(name);
            return path == null ? null : this.LoadFromAssemblyPath(path);
        }

        private static readonly ConcurrentBag<LoadContext> Loaded =
            new ConcurrentBag<LoadContext>();

        /// <summary>
        /// Unloads all created contexts that can be unloaded.
        /// </summary>
        public static void UnloadAll()
        {
            while (Loaded.TryTake(out var context))
            {
                if (context.IsCollectible)
                {
                    context.Unload();
                }
            }
        }

        /// <summary>
        /// Loads an assembly at the given location into a new context.
        /// Adds the specified fallback locations, if any,
        /// to the list of paths where the context will try to look for assemblies that could otherwise not be loaded.
        /// </summary>
        /// <exception cref="FileNotFoundException">File at <paramref name="path"/> does not exist.</exception>
        public static Assembly LoadAssembly(string path, string[]? fallbackPaths = null)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Failed to create contex for \"path\". No such file exists.");
            }
            var context = new LoadContext(path);
            if (fallbackPaths != null)
            {
                context.AddToPath(fallbackPaths);
            }
            Loaded.Add(context);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(path));
            return context.LoadFromAssemblyName(assemblyName);
        }
    }
}
