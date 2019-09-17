// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Build.Locator;


namespace Microsoft.Quantum.QsLanguageServer
{
    enum ConnectionMode
    {
        NamedPipe,
        Socket
    }

    public class Server
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication<Server>();
            app.VersionOption(
                "--version",
                () => typeof(Server).Assembly.GetName().Version.ToString()
            );
            return app.Execute(args);
        }

        [Option(
            "-l|--log",
            Description="Path to log messages to."
        )]
        string LogFile { get; } = null;

        [Option(
            "-p|--port",
            Description="Port to use for TCP/IP connections."
        )]
        int? Port { get; } = null;

        [Option(
            "-w|--writer",
            Description="Named pipe to write to."
        )]
        string WriterPipeName { get; } = null;

        [Option(
            "-r|--reader",
            Description="Named pipe to read from."
        )]
        string ReaderPipeName { get; } = null;

        void Log(object msg)
        {
            // We open and close the file each time; this is not efficient,
            // but prevents locks on the file that would otherwise be very
            // annoying to debug.
            if (LogFile != null)
            {
                using (var writer = new StreamWriter(LogFile, append: true))
                {
                    writer.WriteLine(msg);
                }
            }
        }

        ConnectionMode Validate()
        {
            if (Port != null && (WriterPipeName != null || ReaderPipeName != null))
            {
                Log("[Error] Must specify either port or pipe name, not both.");
                Environment.Exit(-1);
            }

            if (Port == null && (WriterPipeName == null || ReaderPipeName == null))
            {
                Log("[Error] Must specify both writer and reader pipe names.");
                Environment.Exit(-2);
            }

            return Port == null
                ? ConnectionMode.NamedPipe
                : ConnectionMode.Socket;
        }

        public static QsLanguageServer ConnectNamedPipe(string writerName, string readerName)
        {

            var writerPipe = new NamedPipeClientStream(writerName);
            var readerPipe = new NamedPipeClientStream(readerName);

            readerPipe.Connect();
            writerPipe.Connect();
            return new QsLanguageServer(writerPipe, readerPipe);
        }

        private QsLanguageServer ConnectSocket(string hostname = "localhost", int port = 8008)
        {

            try
            {
                Log($"Connecting via socket.");
                var client = new TcpClient(hostname, port);
                var stream = client.GetStream();

                Log($"Connected to {hostname} at port {port}.");
                var lsp = new QsLanguageServer(stream, stream);
                return lsp;
            }
            catch (Exception ex)
            {
                Log($"[ERROR] {ex.Message}");
                Environment.Exit(-2);
                return null;
            }
        }

        int OnExecute()
        {
            MSBuildLocator.RegisterDefaults(); // needed to "configure" the design time build

            QsLanguageServer server = null;
            switch (Validate())
            {
                case ConnectionMode.NamedPipe:
                    server = ConnectNamedPipe(WriterPipeName, ReaderPipeName);
                    break;

                case ConnectionMode.Socket:
                    server = ConnectSocket(port: Port.Value);
                    break;
            }

            Log("Waiting for shutdown...");
            server.WaitForShutdown();

            if (server.ReadyForExit)
            {
                Log("Exiting normally.");
                return 0;
            }
            else
            {
                Log("Exiting abnormally.");
                return 1;
            }
        }
    }
}
