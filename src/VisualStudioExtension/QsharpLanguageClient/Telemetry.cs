// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Telemetry;


namespace Microsoft.Quantum.QsLanguageExtensionVS
{
    public static class Telemetry
    {
        public enum ExtensionEvent
        {
            Activate,           // when the LS gets started (when a .qs file is open)
            LspReady,           // when the LS reports is ready.
            Error
        }

        public const string PUBLISHER = "quantum";
        public const string EXTENSION = "devkit";

        public static readonly Version Version;
        static Telemetry()
        {
            try { Version = typeof(Telemetry).Assembly.GetName().Version; }
            catch { Version = new Version(0, 0); }
        }

        public static string PrefixEventName(string eventName) =>
            $"{PUBLISHER}/{EXTENSION}/{eventName?.ToLowerInvariant()}";

        public static string PrefixProperty(string propName)
        {
            if (propName == null) throw new ArgumentNullException(nameof(propName));
            if (propName.StartsWith(PUBLISHER, StringComparison.InvariantCultureIgnoreCase)) return propName;
            return $"{PUBLISHER}.{EXTENSION}.{propName?.ToLowerInvariant()}";
        }

        /// Does nothing unless the corresponding flag is defined upon compilation.
        /// If the telemetry flag is defined upon compilation, 
        /// sends the telemetry event for the event with the given name and properties.
        /// Ignores any properties where the key is null or "version" (case insensitive).
        /// Adds the version of the extension assembly as version. 
        public static void SendEvent(string name, IEnumerable<KeyValuePair<string, object>> props = null)
        {
#if TELEMETRY
            props = props ?? Enumerable.Empty<KeyValuePair<string, object>>();
            var evt = new TelemetryEvent(PrefixEventName(name));
            foreach (var entry in props.Where(p => !string.IsNullOrEmpty(p.Key)))
            {
                evt.Properties[PrefixProperty(entry.Key)] = entry.Value;
            }
            evt.Properties[PrefixProperty("version")] = Telemetry.Version;

            try { TelemetryService.DefaultSession.PostEvent(evt); }
            catch (Exception ex)
            { Debug.Assert(false, $"error sending telemetry: \n{ex}"); }
#endif
        }

        public static void SendEvent(ExtensionEvent id, params (string, object)[] props) =>
            SendEvent(id.ToString("g"), props.Select(entry => new KeyValuePair<string, object>(entry.Item1, entry.Item2)));
    }
}
