// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Net.Sockets;
using Microsoft.Build.Locator;
using Mono.Options;


namespace Microsoft.Quantum.QsLanguageServer
{
    enum ConnectionMode
    {
        NamedPipe,
        Socket
    }

    public static class Server
    {

        static string logFile = null;
        static int? port = null;

        static string writerPipeName = null;
        static string readerPipeName = null;

        static void Log(object msg)
        {
            // We open and close the file each time; this is not efficient,
            // but prevents locks on the file that would otherwise be very
            // annoying to debug.
            if (logFile != null) {
                using (var writer = new StreamWriter(logFile, append: true))
                {
                    writer.WriteLine(msg);
                }
            }
        }
        
        static ConnectionMode Validate()
        {
            if (port != null && (writerPipeName != null || readerPipeName != null))
            {
                Log("[Error] Must specify either port or pipe name, not both.");
                Environment.Exit(-1);
            }

            if (port == null && (writerPipeName == null || readerPipeName == null))
            {
                Log("[Error] Must specify both writer and reader pipe names.");
                Environment.Exit(-2);
            }

            return port == null 
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

        private static QsLanguageServer ConnectSocket(string hostname = "localhost", int port = 8008)
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

        static int Main(string[] args)
        {
            MSBuildLocator.RegisterDefaults(); // needed to "configure" the design time build

            Log("Called with command line arguments: " + String.Join(" ", args));
            var options = new OptionSet
            {
                { "l|log=", "file to write log messages to.", l => logFile = l },
                { "p|port=", "TCP port to connect to", (int p) => port = p },
                { "w|writer=", "Named pipe to write to", w => writerPipeName = w },
                { "r|reader=", "Named pipe to read from", r => readerPipeName = r }
            };

            List<String> extra = null;
            try { extra = options.Parse(args); } 
            catch (OptionException e)
            { Log(e.Message); }

            QsLanguageServer server = null;
            switch (Validate())
            {
                case ConnectionMode.NamedPipe:
                    server = ConnectNamedPipe(writerPipeName, readerPipeName);
                    break;

                case ConnectionMode.Socket:
                    server = ConnectSocket(port: port.Value);
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
