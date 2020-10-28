// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using Microsoft.Build.Locator;
using Microsoft.Quantum.QsCompiler.CompilationBuilder;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Quantum.QsLanguageServer.Testing
{
    [TestClass]
    public class ProjectLoaderTests
    {
        [AssemblyInitialize]
        public static void SetupMSBuildLocator(TestContext testContext)
        {
            VisualStudioInstance vsi = MSBuildLocator.RegisterDefaults();

            // This is replicating the approach followed in Microsoft.Quantum.QsLanguageServer.Server.Run()
            AssemblyLoadContext.Default.Resolving += (assemblyLoadContext, assemblyName) =>
            {
                string path = Path.Combine(vsi.MSBuildPath, assemblyName.Name + ".dll");
                if (File.Exists(path))
                {
                    return assemblyLoadContext.LoadFromAssemblyPath(path);
                }
                return null;
            };
        }

        private static string ProjectFileName(string project) =>
            Path.Combine("TestProjects", project, $"{project}.csproj");

        private static string SourceFileName(string project, string fileName) =>
            Path.Combine("TestProjects", project, fileName);

        private (string, ProjectInformation?) Context(string project)
        {
            var relativePath = ProjectFileName(project);
            var uri = new Uri(Path.GetFullPath(relativePath));
            return (uri.LocalPath, CompilationContext.Load(uri));
        }

        [TestMethod]
        public void GetGlobalProperties()
        {
            var expectedFramework = "Some-framework";
            var result = ProjectLoader.GlobalProperties(expectedFramework);

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual("false", result["BuildProjectReferences"]);
            Assert.AreEqual("false", result["EnableFrameworkPathOverride"]);
            Assert.AreEqual(expectedFramework, result["TargetFramework"]);
        }

        [TestMethod]
        public void SupportedTargetFrameworks()
        {
            var loader = new ProjectLoader();
            Assert.IsTrue(loader.IsSupportedQsFramework("netstandard2.0"));
            Assert.IsTrue(loader.IsSupportedQsFramework("netstandard2.1"));
            Assert.IsTrue(loader.IsSupportedQsFramework("netcoreapp2.0"));
            Assert.IsTrue(loader.IsSupportedQsFramework("netcoreapp2.1"));
            Assert.IsTrue(loader.IsSupportedQsFramework("netcoreapp2.2"));
            Assert.IsTrue(loader.IsSupportedQsFramework("netcoreapp3.0"));
            Assert.IsTrue(loader.IsSupportedQsFramework("netcoreapp3.1"));
        }

        [TestMethod]
        public void FindProjectTargetFramework()
        {
            void CompareFramework(string project, string? expected)
            {
                var projectFileName = ProjectFileName(project);
                var props = new ProjectLoader().DesignTimeBuildProperties(projectFileName, out var _, (x, y) => (y.Contains('.') ? 1 : 0) - (x.Contains('.') ? 1 : 0));
                if (!props.TryGetValue("TargetFramework", out var actual))
                {
                    actual = null;
                }
                Assert.AreEqual(expected, actual);
            }

            var testProjects = new (string, string?)[]
            {
                ("test1", "netcoreapp2.1"),
                ("test2", "netstandard2.0"),
                ("test3", "netstandard2.0"),
                ("test3", "netstandard2.0"),
                ("test4", "netcoreapp2.0"),
                ("test5", "netcoreapp2.0"),
                ("test6", "netstandard2.0"),
                ("test7", "net461"),
                ("test8", null),
                ("test9", "netcoreapp2.0"),
                ("test10", "netcoreapp2.1"),
                ("test11", "netcoreapp3.0"),
                ("test12", "netstandard2.1"),
                ("test13", "netcoreapp3.1")
            };

            foreach (var (project, framework) in testProjects)
            {
                CompareFramework(project, framework);
            }
        }

        [TestMethod]
        public void LoadNonQsharpProjects()
        {
            var invalidProjects = new string[]
            {
                "test1",
                "test2",
                "test8",
            };

            foreach (var project in invalidProjects)
            {
                var (_, context) = this.Context(project);
                Assert.IsNull(context);
            }
        }

        [TestMethod]
        public void LoadOutdatedQsharpProject()
        {
            var (projectFile, context) = this.Context("test9");
            var projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test9.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            var qsFiles = new string[]
            {
                Path.Combine(projDir, "Operation9.qs"),
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            Assert.IsFalse(context.UsesXunitHelper());
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());
        }

        [TestMethod]
        public void LoadQsharpCoreLibraries()
        {
            var (projectFile, context) = this.Context("test3");
            var projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test3.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            var qsFiles = new string[]
            {
                Path.Combine(projDir, "Operation3a.qs"),
                Path.Combine(projDir, "Operation3b.qs"),
                Path.Combine(projDir, "sub1", "Operation3b.qs"),
                Path.Combine(projDir, "sub1", "sub2", "Operation3a.qs")
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            Assert.IsFalse(context.UsesXunitHelper());
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());

            (projectFile, context) = this.Context("test12");
            projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test12.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            qsFiles = new string[]
            {
                Path.Combine(projDir, "Operation12a.qs"),
                Path.Combine(projDir, "Operation12b.qs"),
                Path.Combine(projDir, "sub1", "Operation12b.qs"),
                Path.Combine(projDir, "sub1", "sub2", "Operation12a.qs")
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            Assert.IsFalse(context.UsesXunitHelper());
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());
        }

        [TestMethod]
        public void LoadQsharpFrameworkLibrary()
        {
            var (projectFile, context) = this.Context("test7");
            var projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test7.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            var qsFiles = new string[]
            {
                Path.Combine(projDir, "Operation.qs")
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            Assert.IsFalse(context.UsesXunitHelper());
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());
        }

        [TestMethod]
        public void LoadQsharpConsoleApps()
        {
            var (projectFile, context) = this.Context("test4");
            var projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test4.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            var qsFiles = new string[]
            {
                Path.Combine(projDir, "Operation4.qs")
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            Assert.IsFalse(context.UsesXunitHelper());
            Assert.IsTrue(context.UsesProject("test3.csproj"));
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());

            (projectFile, context) = this.Context("test10");
            projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test10.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            qsFiles = new string[]
            {
                Path.Combine(projDir, "Operation10.qs")
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());

            (projectFile, context) = this.Context("test11");
            projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test11.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            qsFiles = new string[]
            {
                Path.Combine(projDir, "Operation11.qs")
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());
        }

        [TestMethod]
        public void LoadQsharpUnitTest()
        {
            var (projectFile, context) = this.Context("test5");
            var projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test5.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            var qsFiles = new string[]
            {
                // Compilation target set to none for "Operation5.qs",
                Path.Combine(projDir, "Tests5.qs"),
                Path.Combine(projDir, "test.folder", "Operation5.qs")
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            Assert.IsTrue(context.UsesXunitHelper());
            Assert.IsTrue(context.UsesProject("test3.csproj"));
            Assert.IsTrue(context.UsesProject("test4.csproj"));
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());
        }

        [TestMethod]
        public void LoadQsharpMultiFrameworkLibrary()
        {
            var (projectFile, context) = this.Context("test6");
            var projDir = Path.GetDirectoryName(projectFile) ?? "";
            Assert.IsNotNull(context);
            Assert.AreEqual("test6.dll", Path.GetFileName(context!.Properties.OutputPath));
            Assert.IsTrue((Path.GetDirectoryName(context.Properties.OutputPath) ?? "").StartsWith(projDir));

            var qsFiles = new string[]
            {
                Path.Combine(projDir, "..", "test7", "Operation.qs"), // linked file
                Path.Combine(projDir, "Operation6a.qs"),
                Path.Combine(projDir, "sub1", "Operation6a.qs"),
            };

            Assert.IsTrue(context.UsesIntrinsics());
            Assert.IsTrue(context.UsesCanon());
            Assert.IsFalse(context.UsesXunitHelper());
            Assert.IsTrue(context.UsesProject("test3.csproj"));
            CollectionAssert.AreEquivalent(qsFiles, context.SourceFiles.ToArray());
        }

        [TestMethod]
        public void LoadQsharpTemporaryProject()
        {
            var sourceFile = Path.GetFullPath(SourceFileName("test14", "Operation14.qs"));
            var projectUri = CompilationContext.CreateTemporaryProject(new Uri(sourceFile), "0.12.20072031");
            Assert.IsNotNull(projectUri);

            var qsFiles = new string[] { sourceFile };
            var projectInformation = CompilationContext.Load(projectUri);
            Assert.IsNotNull(projectInformation);
            Assert.IsTrue(projectInformation!.UsesCanon());
            CollectionAssert.AreEquivalent(qsFiles, projectInformation!.SourceFiles.ToArray());
        }
    }

    internal static class CompilationContext
    {
        private static void LogOutput(string msg, MessageType level) =>
            Console.WriteLine($"[{level}]: {msg}");

        internal static ProjectInformation? Load(Uri projectFile) =>
            new EditorState(new ProjectLoader(LogOutput), null, null, null, null, null)
                .QsProjectLoader(projectFile, out var loaded) ? loaded : null;

        internal static Uri CreateTemporaryProject(Uri sourceFile, string sdkVersion) =>
            new EditorState(new ProjectLoader(LogOutput), null, null, null, null, null)
                .QsTemporaryProjectLoader(sourceFile, sdkVersion);

        internal static bool UsesDll(this ProjectInformation info, string dll) => info.References.Any(r => r.EndsWith(dll));

        internal static bool UsesProject(this ProjectInformation info, string projectFileName) => info.ProjectReferences.Any(r => r.EndsWith(projectFileName));

        // NB: We check whether the project uses either the 0.3–0.5 name (Primitives) or the 0.6– name (Intrinsic).
        internal static bool UsesIntrinsics(this ProjectInformation info) => info.UsesDll("Microsoft.Quantum.Intrinsic.dll") || info.UsesDll("Microsoft.Quantum.Primitives.dll");

        internal static bool UsesCanon(this ProjectInformation info) =>
            info.UsesDll("Microsoft.Quantum.Canon.dll") ||
            info.UsesDll("Microsoft.Quantum.Standard.dll");

        internal static bool UsesXunitHelper(this ProjectInformation info) => info.UsesDll("Microsoft.Quantum.Simulation.XUnit.dll");
    }
}
