// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

import * as vscode from 'vscode';
import { getPackageInfo } from './packageInfo';
import TelemetryReporter from 'vscode-extension-telemetry';

export type TelemetryData<T> = { [key: string]: T } | undefined;

export var reporter: TelemetryReporter;

export class Reporter extends vscode.Disposable {
    constructor(ctx: vscode.ExtensionContext) {
        super(() => reporter.dispose());
        let packageInfo = getPackageInfo(ctx);
        if (packageInfo !== undefined && packageInfo.enableTelemetry) {
            reporter = new TelemetryReporter(packageInfo.name, packageInfo.version, packageInfo.aiKey);
        }
    }
}

export namespace ErrorSeverities {
    export const Error = "error";
    export const Critical = "critical";
}

export namespace EventNames {
    export const activate = "activate";
    export const lspReady = "lsp-ready";
    export const lspStopped = "lsp-stopped";
    export const error = "error";
    export const commandExecuted = "command-executed";
}

// @ts-ignore
function hasExtension(extensionId: string): boolean  {
    let ext = vscode.extensions.getExtension(extensionId);
    return ext !== undefined;
}

export function startTelemetry(context: vscode.ExtensionContext) {
    context.subscriptions.push(new Reporter(context));

    // Send initial events.
    if (reporter) reporter.sendTelemetryEvent(EventNames.activate, {}, {});
}

export function sendTelemetryTiming<T>(
        eventName : string,
        action : () => T,
        properties ?: TelemetryData<string>,
        measurements ?: TelemetryData<number>
    ): T {

        let startAction = Date.now();
        let returnValue = action();
        let elapsedTime = Date.now() - startAction;
        if (measurements === undefined) {
            measurements = {};
        }

        measurements["elapsedTime"] = elapsedTime;

        if (reporter) reporter.sendTelemetryEvent(eventName, properties, measurements);

        return returnValue;

}

const PROPERTY_PREFIX = "quantum.devkit.";

function tagTelemetryData<T>(data ?: TelemetryData<T>) : TelemetryData<T> {
    if (data === undefined) {
        return {};
    }

    var taggedData : TelemetryData<T> = {};
    Object
        .keys(data)
        .forEach(
            (key) => {
                var value = data[key];
                var taggedKey: string;
                if (!key.startsWith(PROPERTY_PREFIX)) {
                    taggedKey = PROPERTY_PREFIX + key;
                } else {
                    taggedKey = key;
                }
                taggedData![taggedKey] = value;
            }
        );

    return taggedData;
}

export function sendTelemetryEvent(
    eventName : string,
    properties ?: TelemetryData<string>,
    measurements ?: TelemetryData<number>
) {
    // Ensure that all properties and measurements are tagged.
    if (reporter) {
        reporter.sendTelemetryEvent(
            eventName,
            tagTelemetryData(properties),
            tagTelemetryData(measurements)
        );
    }
}

/**
 * Forwards a telemetry event from the server through to the reporter used by
 * this extension.
 *
 * @param telemetryRequest The request sent by the server.
 */
export function forwardServerTelemetry(telemetryRequest : any) {
    // Define defaults in case that the server didn't send the
    // right information.
    let name = "unknown-server-event";
    let properties: TelemetryData<string> = {};
    let measurements: TelemetryData<number> = {};

    // The LSP does not define what a telemetry request can look
    // like, so it's up to us to define the telemetry structure.
    if ('event' in telemetryRequest) {
        name = telemetryRequest['event'];
    }

    // NB: the LSP API tells us only that telemetryRequest is an
    //     any, so we don't have proper type checking here.
    //     As a work around, we copy properties and measurements out one at a
    //     time, dropping any values of the wrong type, and logging that to the
    //     console.
    if ('properties' in telemetryRequest) {
        Object.keys(telemetryRequest['properties']).forEach(
            key => {
                var prop = telemetryRequest['properties'][key];
                if (typeof prop === "string" && properties !== undefined) {
                    properties[key] = prop;
                } else {
                    console.log(`[qsharp-lsp] Telemetry property ${key} = ${prop} isn't a string.`);
                }
            }
        );
    }
    if ('measurements' in telemetryRequest) {
        Object.keys(telemetryRequest['measurements']).forEach(
            key => {
                var prop = telemetryRequest['measurements'][key];
                if (typeof prop === "number" && measurements !== undefined) {
                    measurements[key] = prop;
                } else {
                    console.log(`[qsharp-lsp] Telemetry measurement ${key} = ${prop} isn't a number.`);
                }
            }
        );
    }

    // TODO: pass more than just the event name.
    sendTelemetryEvent(name, properties, measurements); 
}
