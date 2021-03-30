// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Loader;
using CommandLine;
using CommandLine.Text;
using Microsoft.Build.Locator;

namespace Microsoft.Quantum.QsLanguageServer
{
    public class Server
    {
        public class Options
        {
            // Note: items in one set are mutually exclusive with items from other sets
            protected const string ConnectionViaSocket = "connectionViaSocket";
            protected const string ConnectionViaPipe = "connectionViaPipe";

            [Option(
                'l',
                "log",
                Required = false,
                Default = null,
                HelpText = "Path to log messages to.")]
            public string? LogFile { get; set; }

            [Option(
                'p',
                "port",
                Required = true,
                SetName = ConnectionViaSocket,
                HelpText = "Port to use for TCP/IP connections.")]
            public int Port { get; set; }

            [Option(
                'w',
                "writer",
                Required = true,
                SetName = ConnectionViaPipe,
                HelpText = "Named pipe to write to.")]
            public string? WriterPipeName { get; set; }

            [Option(
                'r',
                "reader",
                Required = true,
                SetName = ConnectionViaPipe,
                HelpText = "Named pipe to read from.")]
            public string? ReaderPipeName { get; set; }
        }

        public enum ReturnCode
        {
            SUCCESS = 0,
            MISSING_ARGUMENTS = 1,
            INVALID_ARGUMENTS = 2,
            MSBUILD_UNINITIALIZED = 3,
            CONNECTION_ERROR = 4,
            UNEXPECTED_ERROR = 100
        }

        private static int LogAndExit(ReturnCode code, string? logFile = null, string? message = null)
        {
            var text = message ?? (
                code == ReturnCode.SUCCESS ? "Exiting normally." :
                code == ReturnCode.MISSING_ARGUMENTS ? "Missing command line options." :
                code == ReturnCode.INVALID_ARGUMENTS ? "Invalid command line arguments. Use --help to see the list of options." :
                code == ReturnCode.MSBUILD_UNINITIALIZED ? "Failed to initialize MsBuild." :
                code == ReturnCode.CONNECTION_ERROR ? "Failed to connect." :
                code == ReturnCode.UNEXPECTED_ERROR ? "Exiting abnormally." : "");
            Log(text, logFile);
            return (int)code;
        }

        public static string? Version =
            typeof(Server).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Server).Assembly.GetName().Version?.ToString();

        public static int Main(string[] args)
        {
            // We need to set the current directory to the same directory of
            // the LanguageServer executable so that it will pick the global.json file
            // and force the MSBuildLocator to use .NET Core SDK 3.1
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            var parser = new Parser(parser => parser.HelpWriter = null); // we want our own custom format for the version info
            var options = parser.ParseArguments<Options>(args);
            return options.MapResult(
                (Options opts) => Run(opts),
                errs => errs.IsVersion()
                    ? LogAndExit(ReturnCode.SUCCESS, message: Version)
                    : LogAndExit(ReturnCode.INVALID_ARGUMENTS, message: HelpText.AutoBuild(options)));
        }

        private static int Run(Options options)
        {
            if (options == null)
            {
                return LogAndExit(ReturnCode.MISSING_ARGUMENTS);
            }

            // In the case where we actually instantiate a server, we need to "configure" the design time build.
            // This needs to be done before any MsBuild packages are loaded.
            try
            {
                VisualStudioInstance vsi = MSBuildLocator.RegisterDefaults();

                // We're using the installed version of the binaries to avoid a dependency between
                // the .NET Core SDK version and NuGet. This is a workaround due to the issue below:
                // https://github.com/microsoft/MSBuildLocator/issues/86
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
            catch (Exception ex)
            {
                // Don't exit here, since exiting without establishing a connection will result in a cryptic failure of the extension.
                // Instead, we proceed to create a server instance and establish the connection.
                // Any errors can then be properly processed via the standard server-client communication as needed.
                Log("[ERROR] MsBuildLocator could not register defaults.", options.LogFile);
                Log(ex, options.LogFile);
            }

            QsLanguageServer server;
            try
            {
                server = options.ReaderPipeName != null && options.WriterPipeName != null
                    ? ConnectViaNamedPipe(options.WriterPipeName, options.ReaderPipeName, options.LogFile)
                    : ConnectViaSocket(port: options.Port, logFile: options.LogFile);
            }
            catch (Exception ex)
            {
                Log("[ERROR] Failed to launch server.", options.LogFile);
                return LogAndExit(ReturnCode.CONNECTION_ERROR, options.LogFile, ex.ToString());
            }

            Log("Listening...", options.LogFile);
            try
            {
                server.CheckDotNetSdkVersion();
                server.WaitForShutdown();
            }
            catch (Exception ex)
            {
                Log("[ERROR] Unexpected error.", options.LogFile);
                return LogAndExit(ReturnCode.UNEXPECTED_ERROR, options.LogFile, ex.ToString());
            }

            return server.ReadyForExit
                ? LogAndExit(ReturnCode.SUCCESS, options.LogFile)
                : LogAndExit(ReturnCode.UNEXPECTED_ERROR, options.LogFile);
        }

        private static void Log(object msg, string? logFile = null)
        {
            if (logFile != null)
            {
                using var writer = new StreamWriter(logFile, append: true);
                writer.WriteLine(msg);
            }
            else
            {
                Console.WriteLine(msg);
            }
        }

        internal static QsLanguageServer ConnectViaNamedPipe(string writerName, string readerName, string? logFile = null)
        {
            Log($"Connecting via named pipe. {Environment.NewLine}ReaderPipe: \"{readerName}\" {Environment.NewLine}WriterPipe:\"{writerName}\"", logFile);
            var writerPipe = new NamedPipeClientStream(writerName);
            var readerPipe = new NamedPipeClientStream(readerName);

            readerPipe.Connect(30000);
            if (!readerPipe.IsConnected)
            {
                Log($"[ERROR] Connection attempted timed out.", logFile);
            }
            writerPipe.Connect(30000);
            if (!writerPipe.IsConnected)
            {
                Log($"[ERROR] Connection attempted timed out.", logFile);
            }
            return new QsLanguageServer(writerPipe, readerPipe);
        }

        internal static QsLanguageServer ConnectViaSocket(string hostname = "localhost", int port = 8008, string? logFile = null)
        {
            Log($"Connecting via socket. {Environment.NewLine}Port number: {port}", logFile);
            Stream? stream = null;
            try
            {
                stream = new TcpClient(hostname, port).GetStream();
            }
            catch (Exception ex)
            {
                Log("[ERROR] Failed to get network stream.", logFile);
                Log(ex.ToString(), logFile);
            }
            return new QsLanguageServer(stream, stream);
        }
    }
}
