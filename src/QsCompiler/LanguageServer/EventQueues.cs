// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Quantum.QsLanguageServer
{
    public class CoalesceingQueue
    {
        private int nrSubscriptions = 0;
        private readonly ConcurrentDictionary<int, (IDisposable, IDisposable)> subscriptions;

        public CoalesceingQueue() =>
            this.subscriptions = new ConcurrentDictionary<int, (IDisposable, IDisposable)>();

        public bool Unsubscribe(int id)
        {
            if (!this.subscriptions.TryRemove(id, out var subscriptions))
            {
                return false;
            }
            subscriptions.Item1.Dispose();
            subscriptions.Item2.Dispose();
            return true;
        }

        public int Subscribe<T>(IObservable<T> observable, Action<IEnumerable<T>> bufferHandler)
        {
            var fileIdle = new Subject<Unit>();
            fileIdle.OnNext(Unit.Default);

            var timer = new System.Timers.Timer(2000);
            timer.Elapsed += (_, __) => fileIdle.OnNext(Unit.Default);
            timer.AutoReset = false;
            timer.Enabled = false;

            var eventSubscription = observable.Subscribe(fileEvent =>
            {
                timer.Stop();
                timer.Start();
            });
            var bufferedSubscription = observable.Buffer(fileIdle).Subscribe(e => bufferHandler(e));

            var id = ++this.nrSubscriptions;
            var subscriptions = (eventSubscription, bufferedSubscription);
            if (!this.subscriptions.TryAdd(id, subscriptions))
            {
                id = -1;
            }
            return id;
        }

        internal static IEnumerable<FileEvent> Coalesce(IEnumerable<FileEvent> events)
        {
            var stacks = new Dictionary<Uri, Stack<FileEvent>>(); // we use one queue for each event source in order to coalesce
            foreach (var e in events)
            {
                if (!stacks.TryGetValue(e.Uri, out var stack))
                {
                    stack = new Stack<FileEvent>();
                    stack.Push(e);
                    stacks.Add(e.Uri, stack);
                    continue;
                }

                var last = stack.Pop();
                var changedEvent = new FileEvent { FileChangeType = FileChangeType.Changed, Uri = e.Uri };

                if (last.FileChangeType == e.FileChangeType)
                {
                    stack.Push(last);
                }
                else if (last.FileChangeType == FileChangeType.Created && e.FileChangeType == FileChangeType.Deleted)
                {
                    stacks.Remove(e.Uri);
                }
                else if (last.FileChangeType == FileChangeType.Deleted && e.FileChangeType == FileChangeType.Created)
                {
                    stack.Push(changedEvent);
                }
                else if (last.FileChangeType == FileChangeType.Created && e.FileChangeType == FileChangeType.Changed)
                {
                    stack.Push(last);
                }
                else if (last.FileChangeType == FileChangeType.Changed && e.FileChangeType == FileChangeType.Deleted)
                {
                    stack.Push(e);
                }
                else
                {
                    stack.Push(last);
                    stack.Push(e);
                }
            }
            return stacks.Values.SelectMany(stack => stack.Reverse()).ToImmutableArray();
        }
    }
}
