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
                ParentName = parentName;
                Name = name;
                UtcStart = DateTime.UtcNow;
                UtcEnd = null;
                DurationInMs = null;
                Watch = Stopwatch.StartNew();
            }

            public void End()
            {
                UtcEnd = DateTime.UtcNow;
                Watch.Stop();
                DurationInMs = Watch.ElapsedMilliseconds;
            }

            public string Key()
            {
                return CompilationProcess.GenerateKey(ParentName, Name);
            }

            public bool IsInProgress()
            {
                return Watch.IsRunning;
            }

            public override string ToString()
            {
                return String.Format("Parent: {0}, Name: {1}, UtcStart: {2}, UtcEnd: {3}, DurationInMs: {4}", ParentName ?? "ROOT", Name, UtcStart, UtcEnd, DurationInMs);
            }
        }

        private class CompilationProcessNode
        {
            public readonly CompilationProcess Process;
            public readonly IDictionary<string, CompilationProcessNode> Children;

            public CompilationProcessNode(CompilationProcess process)
            {
                Process = process;
                Children = new Dictionary<string, CompilationProcessNode>();
            }
        }

        private delegate void CompilationEventTypeHandler(CompilationLoader.CompilationProcessEventArgs eventArgs);

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
                UtcDateTime = DateTime.UtcNow;
                Type = type;
                Key = key;
            }

            public override string ToString()
            {
                return String.Format("Type: {0}, UTC: {1}, Key: {2}", Type, UtcDateTime, Key);
            }
        }

        private static readonly IDictionary<CompilationLoader.CompilationProcessEventType, CompilationEventTypeHandler> CompilationEventTypeHandlers = new Dictionary<CompilationLoader.CompilationProcessEventType, CompilationEventTypeHandler>
        {
            { CompilationLoader.CompilationProcessEventType.Start, CompilationEventStartHandler },
            { CompilationLoader.CompilationProcessEventType.End, CompilationEventEndHandler }
        };

        private static readonly IDictionary<string, CompilationProcess> CompilationProcesses = new Dictionary<string, CompilationProcess>();
        private static readonly IList<CompilationProcessNode> CompilationProcessesForest = new List<CompilationProcessNode>();
        private static readonly IList<Warning> Warnings = new List<Warning>();

        private static void BuildCompilationProcessesForest()
        {
            Queue<CompilationProcessNode> toFindChildrenNodes = new Queue<CompilationProcessNode>();

            // First add the roots of all trees to the forest.

            foreach (KeyValuePair<string, CompilationProcess> entry in CompilationProcesses)
            {
                if (entry.Value.ParentName == null)
                {
                    CompilationProcessNode node = new CompilationProcessNode(entry.Value);
                    CompilationProcessesForest.Add(node);
                    toFindChildrenNodes.Enqueue(node);
                }
            }

            // Build the trees.

            while (toFindChildrenNodes.Count > 0)
            {
                CompilationProcessNode parentNode = toFindChildrenNodes.Dequeue();
                foreach (KeyValuePair<string, CompilationProcess> entry in CompilationProcesses)
                {
                    if (parentNode.Process.Name.Equals(entry.Value.ParentName))
                    {
                        CompilationProcessNode childNode = new CompilationProcessNode(entry.Value);
                        parentNode.Children.Add(childNode.Process.Name, childNode);
                        toFindChildrenNodes.Enqueue(childNode);
                    }
                }
            }
        }

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

            BuildCompilationProcessesForest();

            Console.WriteLine(">> RESULTS <<");
            Console.WriteLine("PROCESSES:");
            Console.WriteLine(CompilationProcessesForest);
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
