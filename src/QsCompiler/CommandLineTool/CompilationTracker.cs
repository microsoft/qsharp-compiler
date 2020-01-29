using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Quantum.QsCompiler;


namespace Microsoft.Quantum.QsCompiler.CommandLineCompiler
{
    public static class CompilationTracker
    {

        private class CompilationProcess
        {
            public readonly string ParentName;
            public readonly string Name;
            public readonly DateTime UtcStart;
            public DateTime? UtcEnd;
            public long? DurationInMs;
            private readonly Stopwatch Watch;

            public static string GenerateKey(string parentName, string name)
            {
                return String.Format("{0}.{1}", parentName ?? "ROOT", name);
            }


            public CompilationProcess(string parentName, string name)
            {
                this.ParentName = parentName;
                this.Name = name;
                this.UtcStart = DateTime.UtcNow;
                this.UtcEnd = null;
                this.DurationInMs = null;
                this.Watch = Stopwatch.StartNew();
            }

            public void End()
            {
                this.UtcEnd = DateTime.UtcNow;
                this.Watch.Stop();
                this.DurationInMs = this.Watch.ElapsedMilliseconds;
            }

            public string Key()
            {
                return CompilationProcess.GenerateKey(ParentName, Name);
            }

            public bool IsInProgress()
            {
                return this.Watch.IsRunning;
            }

            public override string ToString()
            {
                return String.Format("Parent: {0}, Name: {1}, UtcStart: {2}, UtcEnd: {3}, DurationInMs: {4}", ParentName ?? "ROOT", Name, UtcStart, UtcEnd, DurationInMs);
            }
        }

        private enum WarningType
        {
            ProcessAlreadyExists,
            ProcessDoesNotExist,
            ProcessAlreadyEnded,
            ProcessStillInProgress
        }

        private class Warning
        {
            public readonly WarningType Type;
            public readonly DateTime UtcDateTime;
            public readonly string Key;

            public Warning(WarningType type, string key)
            {
                this.UtcDateTime = DateTime.UtcNow;
                this.Type = type;
                this.Key = key;
            }

            public override string ToString()
            {
                return String.Format("Type: {0}, UTC: {1}, Key: {2}", Type, UtcDateTime, Key);
            }
        }

        private static readonly IDictionary<string, CompilationProcess> CompilationProcesses = new Dictionary<string, CompilationProcess>();
        private delegate void CompilationEventTypeHandler(CompilationLoader.CompilationProcessEventArgs eventArgs);
        private static void CompilationEventStartHandler(CompilationLoader.CompilationProcessEventArgs eventArgs)
        {
            string key = CompilationProcess.GenerateKey(eventArgs.ParentName, eventArgs.Name);
            if (CompilationProcesses.ContainsKey(key))
            {
                Warnings.Add(new Warning(WarningType.ProcessAlreadyExists, key));
                return;
            }

            CompilationProcesses.Add(key, new CompilationProcess(eventArgs.ParentName, eventArgs.Name));
        }

        private static void CompilationEventEndHandler(CompilationLoader.CompilationProcessEventArgs eventArgs)
        {
            string key = CompilationProcess.GenerateKey(eventArgs.ParentName, eventArgs.Name);
            if (!CompilationProcesses.TryGetValue(key, out CompilationProcess process))
            {
                Warnings.Add(new Warning(WarningType.ProcessDoesNotExist, key));
                return;
            }

            if (!process.IsInProgress())
            {
                Warnings.Add(new Warning(WarningType.ProcessAlreadyEnded, key));
                return;
            }

            process.End();
        }

        private static readonly IDictionary<CompilationLoader.CompilationProcessEventType, CompilationEventTypeHandler> CompilationEventTypeHandlers = new Dictionary<CompilationLoader.CompilationProcessEventType, CompilationEventTypeHandler>
        {
            { CompilationLoader.CompilationProcessEventType.Start, CompilationEventStartHandler },
            { CompilationLoader.CompilationProcessEventType.End, CompilationEventEndHandler }
        };

        private static readonly IList<Warning> Warnings = new List<Warning>();
        public static void OnCompilationEvent(object sender, CompilationLoader.CompilationProcessEventArgs args)
        {
            CompilationEventTypeHandlers[args.Type](args);
        }

        public static void PublishResults()
        {

            // Validate that all compilation processes are no longer running.
            foreach (KeyValuePair<string, CompilationProcess> entry in CompilationProcesses)
            {
                if (entry.Value.IsInProgress())
                {
                    Warnings.Add(new Warning(WarningType.ProcessStillInProgress, entry.Value.Key()));
                }
            }

            Console.WriteLine(">> RESULTS <<");
            Console.WriteLine("PROCESSES:");
            foreach (KeyValuePair<string, CompilationProcess> entry in CompilationProcesses)
            {
                Console.WriteLine(String.Format("[{0}] {1}", entry.Key, entry.Value));
            }

            Console.WriteLine();

            Console.WriteLine("WARNINGS:");
            foreach (Warning warning in Warnings)
            {
                Console.WriteLine(warning);
            }
        }
    }
}
