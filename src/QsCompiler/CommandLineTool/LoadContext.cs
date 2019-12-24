// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This unfortunately needs to live in this project 
// since the functionality is not available in netstandard2.1.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;


namespace Microsoft.Quantum.QsCompiler
{
    /// <summary>
    /// ...
    /// </summary>
    public class LoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _Resolver;
        internal readonly static List<string> PreloadedAssemblies = new List<string>();

        public LoadContext(string pluginPath) =>
            _Resolver = new AssemblyDependencyResolver(pluginPath);

        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _Resolver.ResolveAssemblyToPath(assemblyName);
            return assemblyPath == null ? null : LoadFromAssemblyPath(assemblyPath);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _Resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            return libraryPath == null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
        }

        public static Assembly LoadAssembly(string path)
        {
            LoadContext context = new LoadContext(path);
            foreach (var dll in LoadContext.PreloadedAssemblies) context.LoadFromAssemblyPath(dll);
            return context.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(path)));
        }
    }
}