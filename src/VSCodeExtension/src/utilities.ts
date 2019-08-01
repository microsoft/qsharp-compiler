// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';

import * as tmp from 'tmp';
import * as net from 'net';

export function tmpName(options?: tmp.SimpleOptions) {
    return new Promise((resolve, reject) => {
        let callback = (err: any, path: string) => {
            if (err === undefined) {
                resolve(path);
            } else {
                reject(err);
            }
        };

        if (options === undefined) {
            tmp.tmpName(callback);
        } else {
            tmp.tmpName(options, callback);
        }
    });
}


/**
 * Given a server, attempts to listen on a given port, incrementing the port
 * number on failure, and yielding the actual port that was used.
 *
 * @param server The server that will be listened on.
 * @param port The first port to try listening on.
 * @param maxPort The highest port number before considering the promise a
 *     failure.
 * @param hostname The hostname that the server should listen on.
 * @returns A promise that yields the actual port number used, or that fails
 *     when net.Server yields an error other than EADDRINUSE or when all ports
 *     up to and including maxPort are already in use.
 */
export function listenOnAvailablePort(server: net.Server, port: number, maxPort: number, hostname: string): Promise<number> {
    return new Promise((resolve, reject) => {
        if (port >= maxPort) {
            reject("Could not find an open port.");
        }
        server.listen(port, hostname)
            .on('listening', () => resolve(port))
            .on('error', (err) => {
                // The 'error' callback lists that err has type Error, which
                // is not specific enough to ensure that the property "code"
                // exists. We cast through any to work around this typing
                // bug, but that's not good at all.
                //
                // To try and mitigate the impact of casting through any,
                // we check explicitly if err.code exists first. In the case
                // that it doesn't, we fail through with err as intended.
                //
                // See
                //     https://github.com/angular/angularfire2/issues/666
                // for another example of a very similar bug.
                if ("code" in err && (err as any).code === "EADDRINUSE") {
                    // portfinder accidentally gave us a port that was already in use,
                    // which can happen due to race conditions. Let's try the next few
                    // ports in case we get lucky.
                    resolve(listenOnAvailablePort(server, port + 1, maxPort, hostname));
                }
                // If we got any other error, reject the promise here; there's
                // nothing else we can do.
                reject(err);
            });
    });
}