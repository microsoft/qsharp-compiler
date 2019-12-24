// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// This unfortunately needs to live in this project 
// since the functionality is not available in netstandard2.1.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;


namespace Microsoft.Quantum.QsCompiler
{
    /// <summary>
    /// Context with some basic handling for loading dependencies. 
    /// Each assembly loaded via LoadAssembly is loaded into its own context.
    /// </summary>
    public class LoadContext : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _Resolver;
        private LoadContext(string pluginPath) =>
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
            var context = new LoadContext(path);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(path));
            return context.LoadFromAssemblyName(assemblyName);
        }
    }
}