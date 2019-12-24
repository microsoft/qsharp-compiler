// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This unfortunately needs to live in this project 
// since the functionality is not available in netstandard2.1.

using System;
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
        private AssemblyDependencyResolver _Resolver;
        private readonly HashSet<string> _FallbackPaths;

        private LoadContext(string parentAssembly)
        {
            this.PathToParentAssembly = parentAssembly ?? throw new ArgumentNullException(nameof(parentAssembly));
            this._Resolver = new AssemblyDependencyResolver(this.PathToParentAssembly);
            this._FallbackPaths = new HashSet<string>();
        }

        public void AddToPath(params string[] paths) =>
            this._FallbackPaths.UnionWith(paths);

        public void RemoveFromPath(params string[] paths) =>
            this._FallbackPaths.RemoveWhere(paths.Contains);

        private string ResolveFromFallbackPaths(AssemblyName name)
        {
            bool MatchByName(string file) =>
                Path.GetFileNameWithoutExtension(file)
                .Equals(name.Name, StringComparison.InvariantCultureIgnoreCase);

            var found = new List<string>();
            foreach (var dir in this._FallbackPaths)
            {
                try { found.AddRange(Directory.GetFiles(dir, ".dll", SearchOption.AllDirectories).Where(MatchByName)); }
                catch { continue; }
            }
            if (found.Count <= 1 || name.Version == null) return found.FirstOrDefault();
            var tempContext = new LoadContext(this.PathToParentAssembly);

            foreach (var file in found)
            {
                var asm = tempContext.LoadFromAssemblyPath(file);
                if (name.Version.Equals(asm.GetName()?.Version)) return file;
            }
            return null;
        }

        protected override Assembly Load(AssemblyName name)
        {
            // We first try to load the assembly specified by the deps.json file of the parent assembly.
            string path = _Resolver.ResolveAssemblyToPath(name);
            // If that fails, we look if we can find a suitable dll in the fallback folder, if one is specified.
            path ??= this.ResolveFromFallbackPaths(name);
            // If not, then the assembly will be loaded within the context of the current app. 
            return path == null ? null : LoadFromAssemblyPath(path);
            // If that fails as well, then the Resolving event is invoked in a last effort before a FileNotFoundException is thrown.
        }

        protected override IntPtr LoadUnmanagedDll(string name)
        {
            string path = _Resolver.ResolveUnmanagedDllToPath(name);
            return path == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(path);
        }

        private static Assembly OnResolving(AssemblyLoadContext context, AssemblyName name) =>
            // Nothing to do here; we prioritize the specified fallback paths over loading from the context of the current app.
            null; 

        public static Assembly LoadAssembly(string path, string[] fallbackPaths = null)
        {
            var context = new LoadContext(path);
            context.Resolving += OnResolving;
            if (fallbackPaths != null) context.AddToPath(fallbackPaths);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(path));
            return context.LoadFromAssemblyName(assemblyName);
        }
    }
}