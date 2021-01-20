// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Diagnostics.FileVersionInfo;


namespace Microsoft.Quantum.QsLanguageExtensionVS
{
    [ContentType("Q#")]
    [Export(typeof(ILanguageClient))]
    public class QsLanguageClient : VisualStudio.Shell.AsyncPackage, ILanguageClient, ILanguageClientCustomMessage
    {
        private readonly long LaunchTime;
        public readonly string LogFile;
        public readonly string LanguageServerPath;

        public static Guid OutputPaneGuid =
            new Guid("C6F36B68-90B4-4A12-BE58-34E3F735B0AE");

        public QsLanguageClient() : base()
        {
            this.LaunchTime = DateTime.Now.Ticks;
            this.LogFile = Path.Combine(Path.GetTempPath(), $"qsp-{LaunchTime}.log");
            this.LanguageServerPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "LanguageServer", "Microsoft.Quantum.QsLanguageServer.exe");
            CustomMessageTarget = new CustomServerNotifications();
        }

        // properties and methods required by ILanguageClientCustomMessage

        public object MiddleLayer => null; // we don't need to intercept messages
        public object CustomMessageTarget { get; }

        /// called third, before initializing the server
        public Task AttachForCustomMessageAsync(JsonRpc rpc) =>
            Task.CompletedTask; // we don't need to send custom messages

        // properties required by ILanguageClient

        public string Name => "Q# Language Extension"; // name as displayed to the user
        public IEnumerable<string> ConfigurationSections => null; // null is fine if the client does not provide settings
        public IEnumerable<string> FilesToWatch => null; // we use our own watcher rather than the one of the LSP Client
        public object InitializationOptions => JObject.FromObject(new
        {
            name = "VisualStudio",
            version = GetVisualStudioVersion()
        });

        // events required by ILanguageClient

        public event AsyncEventHandler<EventArgs> StartAsync;
#pragma warning disable 67
        public event AsyncEventHandler<EventArgs> StopAsync;
#pragma warning restore 67

        // methods required by ILanguageClient

        /// Called second, once the extension has been loaded.
        /// Calls the StartAsync delegate to signal that the language server can/should be started.
        public async Task OnLoadedAsync() =>
            await (StartAsync?.InvokeAsync(this, EventArgs.Empty)).ConfigureAwait(false);

        /// The server only processes any notifications *after* it has been initialized properly.
        /// Hence we need to wait until after initialization to send all solution events that have already been raised, 
        /// and start listening to new ones. 
        public Task OnServerInitializedAsync() =>
            Task.Run(() => Telemetry.SendEvent(Telemetry.ExtensionEvent.LspReady));

        /// We create a new pane to show the encountered exception. 
        public async Task OnServerInitializeFailedAsync(Exception ex)
        {
            await VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var outWindow = GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;

            var windowTitle = this.Name;
            outWindow.CreatePane(ref OutputPaneGuid, windowTitle, 1, 1);
            outWindow.GetPane(ref OutputPaneGuid, out var customPane);

            var messages = new[]
            {
                $"Server initialization failed.",
                $"Path to the language server executable: \"{this.LanguageServerPath}\"",
                $"Path to the log file: \"{this.LogFile}\"", 
                ex.ToString()
            };
            customPane.OutputString(String.Join(Environment.NewLine, messages)); 
            customPane.Activate(); // brings the pane into view
        }

        /// Invoking the StartAsync event signals that the language server should be started, and triggers a call to this routine.  
        /// This routine contains the logic to start the server and estabilishes a connection that is returned (exceptions here are shown in the info bar of VS).
        public async Task<Connection> ActivateAsync(CancellationToken token) 
        {
            await Task.Yield();
            Telemetry.SendEvent(Telemetry.ExtensionEvent.Activate);
            try
            {
                #if MANUAL
                string ServerReaderPipe = $"QsLanguageServerReaderPipe";
                string ServerWriterPipe = $"QsLanguageServerWriterPipe";
                #else

                string ServerReaderPipe = $"QsLanguageServerReaderPipe{this.LaunchTime}";
                string ServerWriterPipe = $"QsLanguageServerWriterPipe{this.LaunchTime}";
                ProcessStartInfo info = new ProcessStartInfo
                {
                    FileName = LanguageServerPath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = $"--writer={ServerWriterPipe} --reader={ServerReaderPipe} --log={this.LogFile}"
                };

                Process process = new Process { StartInfo = info };
                if (!process.Start()) return null;
                #endif

                var bufferSize = 256;
                var pipeSecurity = new PipeSecurity();
                var id = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null); // I don't think WorldSid ("Everyone") is necessary 
                pipeSecurity.AddAccessRule(new PipeAccessRule(id, PipeAccessRights.ReadWrite, System.Security.AccessControl.AccessControlType.Allow));
                var readerPipe = new NamedPipeServerStream(ServerWriterPipe, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, bufferSize, bufferSize, pipeSecurity);
                var writerPipe = new NamedPipeServerStream(ServerReaderPipe, PipeDirection.InOut, 4, PipeTransmissionMode.Message, PipeOptions.Asynchronous, bufferSize, bufferSize, pipeSecurity);

                await readerPipe.WaitForConnectionAsync(token).ConfigureAwait(true);
                await writerPipe.WaitForConnectionAsync(token).ConfigureAwait(true);
                return new Connection(readerPipe, writerPipe);
            }
            catch (Exception e)
            {
                Telemetry.SendEvent(Telemetry.ExtensionEvent.Error, ("id", e.GetType().Name), ("reason", e.Message));
                throw;
            }
        }

        /// The list of server notifications not part of the default LanguageServer Protocol:
        class CustomServerNotifications
        {
            [JsonRpcMethod("telemetry/event")]
            public void OnTelemetry(JToken args)
            {
                try
                {
                    var name = (string)args["event"];
                    var props = args["properties"]?.ToObject<Dictionary<string, object>>();
                    Telemetry.SendEvent(name, props);
                }
                catch (Exception ex)
                { Debug.Assert(false, $"error sending telemetry: \n{ex}"); }
            }
        }

        /// Setup to get the VS version number including the correct minor version, 
        /// since the DTE.Version does not seem to accurately reflect that. 
        private static string GetVisualStudioVersion()
        {
            FileVersionInfo versionInfo;
            try
            {
                var msenvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "msenv.dll");
                versionInfo = GetVersionInfo(msenvPath);
            }
            catch (FileNotFoundException)
            { return null; }

            // Extract the version number from the string in the format "D16.2", "D16.3", etc.
            var version = Regex.Match(versionInfo.FileVersion, @"D([\d\.]+)");
            return version.Success ? version.Groups[1].Value : null;
        }
    }
}
