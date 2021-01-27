// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Microsoft.Quantum.QsCompiler
{
    public static class ProcessRunner
    {
        /// <summary>
        /// Starts the given process and accumulates the received output and error data in the given StringBuilders.
        /// Returns true if the process completed within the specified time without throwing an exception, and false otherwise.
        /// Any thrown exception is returned as out parameter.
        /// </summary>
        public static bool Run(Process process, StringBuilder output, StringBuilder error, out Exception? ex, int timeout)
        {
            using (var outputWaitHandle = new AutoResetEvent(false))
            using (var errorWaitHandle = new AutoResetEvent(false))
            {
                void AddOutput(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data == null)
                    {
                        outputWaitHandle.Set();
                    }
                    else
                    {
                        output.AppendLine(e.Data);
                    }
                }

                void AddError(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data == null)
                    {
                        errorWaitHandle.Set();
                    }
                    else
                    {
                        error.AppendLine(e.Data);
                    }
                }

                process.OutputDataReceived += AddOutput;
                process.ErrorDataReceived += AddError;

                try
                {
                    ex = null;
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                    return process.WaitForExit(timeout)
                        && outputWaitHandle.WaitOne()
                        && errorWaitHandle.WaitOne();
                }
                catch (Exception e)
                {
                    ex = e;
                }
                finally
                {
                    // unsubscribe such that the AutoResetEvents are not accessed after disposing
                    process.OutputDataReceived -= AddOutput;
                    process.ErrorDataReceived -= AddError;
                }
                return ex == null;
            }
        }

        /// <summary>
        /// Starts and runs a process invoking the given command with the given arguments.
        /// If a dictionary of environment variables and their desired values is specified,
        /// sets these environment variables prior to execution and resets them afterwards.
        /// Accumulates the received output and error data in the respective StringBuilder and returns them as out parameters.
        /// Returns true if the process completed within the specified time without throwing an exception, and false otherwise.
        /// Returns the exit code of the process as well as any thrown exception as out parameter.
        /// </summary>
        public static bool Run(
            string command,
            string args,
            out StringBuilder outstream,
            out StringBuilder errstream,
            out int exitCode,
            out Exception? ex,
            IDictionary<string, string>? envVariables = null,
            int timeout = 10000)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var origEnvVariables = new Dictionary<string, string>();
            foreach (var entry in envVariables ?? new Dictionary<string, string>())
            {
                var orig = Environment.GetEnvironmentVariable(entry.Key);
                origEnvVariables.Add(entry.Key, orig);
                Environment.SetEnvironmentVariable(entry.Key, entry.Value);
            }

            (outstream, errstream) = (new StringBuilder(), new StringBuilder());
            try
            {
                var exited = Run(process, outstream, errstream, out ex, timeout);
                exitCode = process.ExitCode;
                return exited;
            }
            finally
            {
                foreach (var entry in origEnvVariables)
                {
                    Environment.SetEnvironmentVariable(entry.Key, entry.Value);
                }
            }
        }
    }
}
