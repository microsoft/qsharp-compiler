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
            ProcessAlreadyEnded
        }

        private class Warning
        {
            public readonly WarningType Type;
            public readonly DateTime UtcDateTime;
            public readonly CompilationLoader.CompilationProcessEventArgs Args;

            public Warning(WarningType type, CompilationLoader.CompilationProcessEventArgs args)
            {
                this.UtcDateTime = DateTime.UtcNow;
                this.Type = type;
                this.Args = args;
            }
        }

        private static readonly IDictionary<string, CompilationProcess> CompilationProcesses = new Dictionary<string, CompilationProcess>();
        private delegate void CompilationEventTypeHandler(CompilationLoader.CompilationProcessEventArgs eventArgs);
        private static void CompilationEventStartHandler(CompilationLoader.CompilationProcessEventArgs eventArgs)
        {
            string key = String.Format("{0}.{1}", eventArgs.ParentName ?? "ROOT", eventArgs.Name);
            // ToDo: remove.
            Console.WriteLine(key);
            if (CompilationProcesses.ContainsKey(key))
            {
                Warnings.Add(new Warning(WarningType.ProcessAlreadyExists, eventArgs));
                return;
            }

            CompilationProcesses.Add(key, new CompilationProcess(eventArgs.ParentName, eventArgs.Name));
        }

        private static void CompilationEventEndHandler(CompilationLoader.CompilationProcessEventArgs eventArgs)
        {
            string key = String.Format("{0}.{1}", eventArgs.ParentName ?? "ROOT", eventArgs.Name);
            // ToDo: remove.
            Console.WriteLine(key);
            if (!CompilationProcesses.TryGetValue(key, out CompilationProcess process))
            {
                Warnings.Add(new Warning(WarningType.ProcessDoesNotExist, eventArgs));
                return;
            }

            if (!process.IsInProgress())
            {
                Warnings.Add(new Warning(WarningType.ProcessAlreadyEnded, eventArgs));
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

            Console.WriteLine("\n>> RESULTS <<");
            foreach (KeyValuePair<string, CompilationProcess> entry in CompilationProcesses)
            {
                Console.WriteLine(String.Format("[{0}] {1}", entry.Key, entry.Value));
            }
        }
    }
}
