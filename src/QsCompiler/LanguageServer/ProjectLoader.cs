// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsLanguageServer
{
    /// <summary>
    /// Note that this class is *not* threadsafe,
    /// and design time builds will fail if a (design time) build is already in progress.
    /// </summary>
    internal class ProjectLoader
    {
        public readonly Action<string, MessageType> Log;

        public ProjectLoader(Action<string, MessageType>? log = null) =>
            this.Log = log ?? ((_, __) => { });

        // possibly configurable properties

        /// <summary>
        /// Returns a dictionary with global properties used to load projects at runtime.
        /// BuildProjectReferences is set to false such that references are not built upon ResolveAssemblyReferencesDesignTime.
        /// </summary>
        internal static Dictionary<string, string> GlobalProperties(string? targetFramework = null)
        {
            var props = new Dictionary<string, string>
            {
                ["BuildProjectReferences"] = "false",
                ["EnableFrameworkPathOverride"] = "false" // otherwise msbuild fails on .net 461 projects
            };
            if (targetFramework != null)
            {
                // necessary for multi-framework projects.
                props["TargetFramework"] = targetFramework;
            }
            return props;
        }

        private static readonly Regex TargetFrameworkMoniker =
            new Regex(@"(netstandard[1-9]\.[0-9])|(netcoreapp[1-9]\.[0-9])|(net[1-9][0-9][0-9]?)");

        private readonly ImmutableArray<string> supportedQsFrameworks =
            ImmutableArray.Create("netstandard2.", "netcoreapp2.", "netcoreapp3.");

        /// <summary>
        /// Returns true if the given framework is officially supported for Q# projects.
        /// </summary>
        public bool IsSupportedQsFramework(string framework) =>
            framework != null
            ? this.supportedQsFrameworks.Any(framework.ToLowerInvariant().StartsWith)
            : false;

        /// <summary>
        /// contains a list of Properties from the project that we want to track e.g. for telemetry.
        /// </summary>
        private static readonly IEnumerable<string> PropertiesToTrack =
            new string[] { "QsharpLangVersion" };

        /// <summary>
        /// Returns true if the package with the given name should be tracked.
        /// </summary>
        private static bool GeneratePackageInfo(string packageName) =>
            packageName.StartsWith("microsoft.quantum.", StringComparison.InvariantCultureIgnoreCase);

        // general purpose routines

        /// <summary>
        /// Returns all targeted frameworks of the given project.
        /// IMPORTANT: currently only supports .net core-style projects.
        /// </summary>
        private static string[] TargetedFrameworks(Project project)
        {
            // this routine does not work in full generality, but it will do for now for our purposes
            var evaluatedProps = project.Properties.Where(p => p.Name?.ToLowerInvariant()?.StartsWith("targetframework") ?? false);
            return evaluatedProps
                .SelectMany(p => TargetFrameworkMoniker.Matches(p.EvaluatedValue.ToLowerInvariant())
                .Where(m => m.Success).Select(m => m.Value))
                .ToArray();
        }

        /// <summary>
        /// Returns a dictionary with the properties used for design time builds of the project corresponding to the given project file.
        /// Chooses a target framework for the build properties according to the given comparison.
        /// Chooses the first target framework is no comparison is given.
        /// Logs a suitable error is no target framework can be determined.
        /// Returns a dictionary with additional project information (e.g. for telemetry) as out parameter.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="projectFile"/> is null or does not exist.</exception>
        internal IDictionary<string, string> DesignTimeBuildProperties(
            string projectFile,
            out Dictionary<string, string?> metadata,
            Comparison<string>? preferredFramework = null)
        {
            if (!File.Exists(projectFile))
            {
                throw new ArgumentException("given project file is null or does not exist", nameof(projectFile));
            }
            (string?, Dictionary<string, string?>) FrameworkAndMetadata(Project project)
            {
                string? GetVersion(ProjectItem item) => item.DirectMetadata
                    .FirstOrDefault(data => data.Name.Equals("Version", StringComparison.InvariantCultureIgnoreCase))?.EvaluatedValue;
                var packageRefs = project.Items.Where(item =>
                    item.ItemType == "PackageReference" && GeneratePackageInfo(item.EvaluatedInclude))
                    .Select(item => (item.EvaluatedInclude, GetVersion(item)));
                var trackedProperties = project.Properties.Where(p =>
                    p?.Name != null && PropertiesToTrack.Contains(p.Name, StringComparer.InvariantCultureIgnoreCase))
                    .Select(p => (p.Name.ToLowerInvariant(), p.EvaluatedValue));

                var projInfo = new Dictionary<string, string?>();
                foreach (var (package, version) in packageRefs)
                {
                    projInfo[$"pkgref.{package}"] = version;
                }
                foreach (var (name, value) in trackedProperties)
                {
                    projInfo[name] = value;
                }
                projInfo["projectNameHash"] = this.GetProjectNameHash(projectFile);

                var frameworks = TargetedFrameworks(project).ToList();
                if (preferredFramework != null)
                {
                    frameworks.Sort(preferredFramework);
                }
                return (frameworks.FirstOrDefault(), projInfo);
            }

            var info = LoadAndApply(projectFile, GlobalProperties(), FrameworkAndMetadata);
            metadata = info.Item2;
            return GlobalProperties(info.Item1).ToImmutableDictionary();
        }

        /// <summary>
        /// Returns a 1-way hash of the project file name so it can be sent with telemetry.
        /// if any exception is thrown, it just logs the error message and returns an empty string.
        /// </summary>
        internal string GetProjectNameHash(string projectFile)
        {
            try
            {
                using (SHA256 hashAlgorithm = SHA256.Create())
                {
                    string fileName = Path.GetFileName(projectFile);
                    byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(fileName));
                    var sBuilder = new StringBuilder();
                    for (int i = 0; i < data.Length; i++)
                    {
                        sBuilder.Append(data[i].ToString("x2"));
                    }
                    return sBuilder.ToString();
                }
            }
            catch (Exception e)
            {
                this.Log($"Error creating hash for project name '{projectFile}': {e.Message}", MessageType.Warning);
                return string.Empty;
            }
        }

        /// <summary>
        /// Loads the project corresponding to the given project file with the given properties,
        /// applies the given query to it, and unloads it. Returns the result of the query.
        /// NOTE: unloads the GlobalProjectCollection to force a cache clearing.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="projectFile"/> is null or does not exist.</exception>
        private static T LoadAndApply<T>(string projectFile, IDictionary<string, string> properties, Func<Project, T> query)
        {
            if (!File.Exists(projectFile))
            {
                throw new ArgumentException("given project file is null or does not exist", nameof(projectFile));
            }

            Project? project = null;
            try
            {
                // Unloading the project unloads the project but *doesn't* clear the cache to be resilient to inconsistent states.
                // Hence we actually need to unload all projects, which does make sure the cache is cleared and changes on disk are reflected.
                // See e.g. https://github.com/Microsoft/msbuild/issues/795
                ProjectCollection.GlobalProjectCollection.UnloadAllProjects(); // needed due to the caching behavior of MS build
                project = new Project(projectFile, properties, ToolLocationHelper.CurrentToolsVersion);
                return query(project);
            }
            finally
            {
                if (project != null)
                {
                    ProjectCollection.GlobalProjectCollection?.UnloadProject(project);
                }
            }
        }

        // routines for loading and processing information from Q# projects specifically

        /// <summary>
        /// Loads the project for the given project file, restores all packages,
        /// and builds the target ResolveAssemblyReferencesDesignTime, logging suitable errors in the process.
        /// If the built project instance is recognized as a valid Q# project by the server, returns the built project instance.
        /// Returns null if this is not the case, or if the given project file is null or does not exist.
        /// Returns a dictionary with additional project information (e.g. for telemetry) as out parameter.
        /// </summary>
        private ProjectInstance? QsProjectInstance(string projectFile, out Dictionary<string, string?> metadata)
        {
            metadata = new Dictionary<string, string?>();
            if (!File.Exists(projectFile))
            {
                return null;
            }
            var loggers = new ILogger[] { new Utils.MSBuildLogger(this.Log) };
            int PreferSupportedFrameworks(string x, string y) => (this.IsSupportedQsFramework(y) ? 1 : 0) - (this.IsSupportedQsFramework(x) ? 1 : 0);
            var properties = this.DesignTimeBuildProperties(projectFile, out metadata, PreferSupportedFrameworks);

            // restore project (requires reloading the project after for the restore to take effect)
            var succeed = LoadAndApply(projectFile, properties, project =>
                project.CreateProjectInstance().Build("Restore", loggers));
            if (!succeed)
            {
                this.Log($"Failed to restore project '{projectFile}'.", MessageType.Error);
            }

            // build the project instance and returns it if it is indeed a Q# project
            return LoadAndApply(projectFile, properties, project =>
            {
                var instance = project.CreateProjectInstance();
                succeed = instance.Build("ResolveAssemblyReferencesDesignTime", loggers);
                if (!succeed)
                {
                    this.Log($"Failed to resolve assembly references for project '{projectFile}'.", MessageType.Error);
                }
                return instance.Targets.ContainsKey("QsharpCompile") ? instance : null;
            });
        }

        /// <summary>
        /// Returns the project instance for the given project file with all assembly references resolved,
        /// if the given project is recognized as a valid Q# project by the server, and null otherwise.
        /// Returns null without logging anything if the given project file does not end in .csproj.
        /// Returns a dictionary with additional project information (e.g. for telemetry) as out parameter.
        /// Logs suitable messages using the given log function if the project file cannot be found, or if the design time build fails.
        /// Logs whether or not the project is recognized as Q# project.
        /// </summary>
        public ProjectInstance? TryGetQsProjectInstance(string projectFile, out Dictionary<string, string?> metadata)
        {
            metadata = new Dictionary<string, string?>();
            if (!projectFile.ToLowerInvariant().EndsWith(".csproj"))
            {
                return null;
            }

            ProjectInstance? instance = null;
            try
            {
                instance = this.QsProjectInstance(projectFile, out metadata);
            }
            catch (Exception ex)
            {
                this.Log($"Error on loading project '{projectFile}': {ex.Message}.", MessageType.Error);
            }

            if (!File.Exists(projectFile))
            {
                this.Log($"Could not find project file '{projectFile}'.", MessageType.Warning);
            }
            else if (instance == null)
            {
                this.Log($"Ignoring non-Q# project '{projectFile}'.", MessageType.Log);
            }
            else
            {
                this.Log($"Discovered Q# project '{projectFile}'.", MessageType.Log);
            }
            return instance;
        }
    }
}
