// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using CommandLine;
using CommandLine.Text;
using Microsoft.Build.Locator;


namespace Microsoft.Quantum.QsLanguageServer
{
    public class Server
    {
        public enum ReturnCode
        { 
            SUCCESS = 0,
            MISSING_ARGUMENTS = 1,
            INVALID_ARGUMENTS = 2,
            UNEXPECTED_ERROR = 100
        }

        private enum ConnectionMode
        {
            NamedPipe,
            Socket
        }

        public class Options
        {
            // Note: items in one set are mutually exclusive with items from other sets
            protected const string CONNECTION_VIA_SOCKET = "connectionViaSocket";
            protected const string CONNECTION_VIA_PIPE = "connectionViaPipe";

            [Option('l', "log", Required = false, Default = null,
            HelpText = "Path to log messages to.")]
            public string LogFile { get; set; }

            [Option('p', "port", Required = true, SetName = CONNECTION_VIA_SOCKET,
            HelpText = "Port to use for TCP/IP connections.")]
            public int Port { get; set; }

            [Option('w', "writer", Required = true, SetName = CONNECTION_VIA_PIPE,
            HelpText = "Named pipe to write to.")]
            public string WriterPipeName { get; set; }

            [Option('r', "reader", Required = true, SetName = CONNECTION_VIA_PIPE,
            HelpText = "Named pipe to read from.")]
            public string ReaderPipeName { get; set; }
        }


        public static int Main(string[] args)
        {
            var parser = new Parser(parser => parser.HelpWriter = null); // we want our own custom format for the version info
            var options = parser.ParseArguments<Options>(args);
            return options.MapResult(
                (Options opts) => Run(opts),
                (errs =>
                {
                    if (errs.IsVersion()) Log(Version); 
                    else Log(HelpText.AutoBuild(options));
                    return errs.IsVersion() 
                        ? (int)ReturnCode.SUCCESS 
                        : (int)ReturnCode.INVALID_ARGUMENTS;
                })
            );
        }


        private static int Run(Options options)
        {
            if (options == null)
            {
                Log("missing command line options");
                return (int)ReturnCode.MISSING_ARGUMENTS;
            }

            // In the case where we actually instantiate a server, we need to "configure" the design time build. 
            // This needs to be done before any MsBuild packages are loaded. 
            MSBuildLocator.RegisterDefaults();

            var connectionMode = options.ReaderPipeName == null || options.WriterPipeName == null
                ? ConnectionMode.Socket
                : ConnectionMode.NamedPipe;

            QsLanguageServer server = null;
            switch (connectionMode)
            {
                case ConnectionMode.NamedPipe:
                    server = ConnectViaNamedPipe(options.WriterPipeName, options.ReaderPipeName, options.LogFile);
                    break;

                case ConnectionMode.Socket:
                    server = ConnectViaSocket(port: options.Port, logFile: options.LogFile);
                    break;
            }

            Log("Waiting for shutdown...", options.LogFile);
            server.WaitForShutdown();

            if (server.ReadyForExit)
            {
                Log("Exiting normally.", options.LogFile);
                return (int)ReturnCode.SUCCESS;
            }
            else
            {
                Log("Exiting abnormally.", options.LogFile);
                return (int)ReturnCode.UNEXPECTED_ERROR;
            }
        }

        public static string Version =
            typeof(Server).Assembly.GetName().Version?.ToString();

        private static void Log(object msg, string logFile = null)
        {
            if (logFile != null)
            {
                using var writer = new StreamWriter(logFile, append: true);
                writer.WriteLine(msg);
            }
            else Console.WriteLine(msg);
        }

        internal static QsLanguageServer ConnectViaNamedPipe(string writerName, string readerName, string logFile = null)
        {
            Log($"Connecting via named pipe.", logFile);
            var writerPipe = new NamedPipeClientStream(writerName);
            var readerPipe = new NamedPipeClientStream(readerName);

            Log($"Connecting to reader pipe \"{readerName}\".", logFile);
            readerPipe.Connect();
            Log($"Connecting to writer pipe \"{writerName}\".", logFile);
            writerPipe.Connect();
            return new QsLanguageServer(writerPipe, readerPipe);
        }

        internal static QsLanguageServer ConnectViaSocket(string hostname = "localhost", int port = 8008, string logFile = null)
        {
            try
            {
                Log($"Connecting via socket.", logFile);
                var client = new TcpClient(hostname, port);
                var stream = client.GetStream();

                Log($"Connected to {hostname} at port {port}.", logFile);
                var lsp = new QsLanguageServer(stream, stream);
                return lsp;
            }
            catch (Exception ex)
            {
                Log($"[ERROR] {ex.Message}", logFile);
                Environment.Exit((int)ReturnCode.UNEXPECTED_ERROR); 
                return null;
            }
        }
    }
}
