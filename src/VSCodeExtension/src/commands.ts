// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
import * as vscode from 'vscode';
import * as cp from 'child_process';
import { DefaultAzureCredential } from "@azure/identity";
import { QuantumJobClient } from "@azure/quantum-jobs";


import { DotnetInfo, findIQSharpVersion } from './dotnet';
import { IPackageInfo } from './packageInfo';
import * as semver from 'semver';
import { promisify } from 'util';
import { QSharpGenerator } from './yeoman-generator';

import * as hardwareConfigs from "./hardwareConfigs.json";

import * as yeoman from 'yeoman-environment';

import {ContainerClient, BlockBlobClient} from "@azure/storage-blob";
import * as pako from 'pako';

type accountInfo = {
    subscriptionId: string;
    resourceGroupName: string;
    workspaceName: string,
    location: string
  };

export function registerCommand(context: vscode.ExtensionContext, name: string, action: () => void) {
    context.subscriptions.push(
        vscode.commands.registerCommand(
            name,
            () => {
                action();
            }
        )
    )
}

export async function createNewProject(context: vscode.ExtensionContext) {
    let env = yeoman.createEnv();
    env.registerStub(QSharpGenerator, 'qsharp:app');
    // Disable type checking on the env object at this point due to
    // https://github.com/yeoman/environment/issues/273.
    let anyEnv = env as any;
    let err = await anyEnv.run('qsharp:app', {
        extensionPath: context.extensionPath
    });
    if (err) {
        let errorMessage = err.name + ": " + err.message;
        console.log(errorMessage);
        vscode.window.showErrorMessage(errorMessage);
    }
}

export function installTemplates(dotNetSdk: DotnetInfo, packageInfo?: IPackageInfo) {
    let packageVersion =
        packageInfo?.nugetVersion
        ? `::${packageInfo!.nugetVersion}`
        : "";
    let proc = cp.spawn(
        dotNetSdk.path,
        ["new", "--install", `Microsoft.Quantum.ProjectTemplates${packageVersion}`]
    );

    let errorMessage = "";
    proc.stderr.on(
        'data', data => {
            errorMessage = errorMessage + data;
        }
    );
    proc.stdout.on(
        'data', data => {
            console.log("" + data);
        }
    )

    proc.on(
        'exit',
        (code, signal) => {
            console.log("dotnet new --install stderr:", errorMessage);
            if (code === 0) {
                vscode.window.showInformationMessage(
                    "Project templates installed successfully."
                );
            } else {
                vscode.window.showErrorMessage(
                    `.NET Core SDK exited with code ${code} when installing project templates:\n${errorMessage}`
                );
            }
        }
    );
}

export function openDocumentationHome() {
    return vscode.env.openExternal(
        vscode.Uri.parse("https://docs.microsoft.com/azure/quantum/")
    );
}

export function installOrUpdateIQSharp(dotNetSdk: DotnetInfo, requiredVersion?: string) {
    findIQSharpVersion()
        .then(
            iqsharpVersion => {
                if (iqsharpVersion !== undefined && iqsharpVersion["iqsharp"] !== undefined) {
                    // We got a version, so let's check if it's up to date or not.
                    // If it is up to date, we print out that this is the case and resolve the
                    // promise immediately.
                    if (requiredVersion === undefined || semver.gte(iqsharpVersion["iqsharp"], requiredVersion)) {
                        vscode.window.showInformationMessage(`Currently IQ# version is up to date (${iqsharpVersion["iqsharp"]}).`);
                        return false;
                    }

                    // If we made it here, we need to install IQ#. This can fail if it's already installed.
                    // While dotnet does offer an update command, it's often more reliable to just uninstall and reinstall.
                    // Thus, we uninstall here before proceeding to the install step below.
                    return promisify(cp.exec)(
                        `"${dotNetSdk.path}" tool uninstall --global Microsoft.Quantum.IQSharp`
                    )
                    .then(() => true);
                }
                return true;
            }
        )
        .then(
            needToInstall => {
                if (needToInstall) {
                    let versionSpec =
                        requiredVersion === undefined
                        ? ""
                        : `::${requiredVersion}`;
                    return promisify(cp.exec)(
                        `"${dotNetSdk.path}" tool install --global Microsoft.Quantum.IQSharp${versionSpec}`
                    )
                    .then(() => {
                        // Check what version actually got installed and report that back.
                        findIQSharpVersion()
                            .then(installedVersion => {
                                if (installedVersion === undefined) {
                                    throw new Error("Could not detect IQ# version after installing.");
                                }
                                if (installedVersion["iqsharp"] === undefined) {
                                    throw new Error("Newly installed IQ# did not report a version.");
                                }
                                vscode.window.showInformationMessage(`Successfully installed IQ# version ${installedVersion["iqsharp"]}`);
                            });
                    });
                }
            }
        )
        .catch(
            reason => {
                vscode.window.showWarningMessage(`Could not install IQ#:\n${reason}`);
            }
        );
}

export async function connectToAzureAccount(context: vscode.ExtensionContext) {
    const subscriptionId = await vscode.window.showInputBox({prompt:"Enter subscription ID",ignoreFocusOut:true});
    const resourceGroupName = subscriptionId && await vscode.window.showInputBox({prompt:"Enter Resource Group Name",ignoreFocusOut:true});
    const workspaceName = resourceGroupName && await vscode.window.showInputBox({prompt:"Enter Workspace Name",ignoreFocusOut:true});
    const location = workspaceName && await vscode.window.showInputBox({prompt:"Enter location",ignoreFocusOut:true});

    if (!location){
        return;
    }

    context.workspaceState.update("accountInfo",
    {"subscriptionId": subscriptionId,
    "resourceGroupName": resourceGroupName,
    "workspaceName": workspaceName,
    "location": location
});

}
export async function disconnectFromAzureAccount(context: vscode.ExtensionContext) {

    context.workspaceState.update("accountInfo", undefined);
    vscode.window.showInformationMessage("Successfully Disconnected");
}


export async function submitJob(context: vscode.ExtensionContext) {
    const credential = new DefaultAzureCredential();
    // Create a QuantumJobClient
    let accountInfo: accountInfo | undefined = context.workspaceState.get("accountInfo");
    if (accountInfo === undefined){
        await connectToAzureAccount(context);
        accountInfo = context.workspaceState.get("accountInfo");
    }
    // TODO ADD FUNCTIONALITY TO ALLOW USER TO RETRY LOGIN STEP
    if (accountInfo ===undefined){
        vscode.window.showWarningMessage(`Could not connect to Azure Account`);
        return;
    }
    const subscriptionId:string = accountInfo["subscriptionId"];
    const resourceGroupName:string = accountInfo["resourceGroupName"];
    const workspaceName:string =  accountInfo["workspaceName"];
    // how should we handle containter??
    const storageContainerName = "mycontainerforjs";
    const location: string =  accountInfo["location"];
    const endpoint = "https://" + location + ".quantum.azure.com";


    const quantumJobClient = new QuantumJobClient(
        credential,
        subscriptionId,
        resourceGroupName,
        workspaceName,
        {
        endpoint: endpoint,
        credentialScopes: "https://quantum.microsoft.com/.default"
        }
    );
    // Get container Uri with SAS key
    const containerUri: string | undefined = (
        await quantumJobClient.storage.sasUri({
        containerName: storageContainerName
    })
    ).sasUri;

    if (containerUri === undefined){
        vscode.window.showWarningMessage(`Could not connect to storage container`);
        return;
    }
    // Create container if not exists
    const containerClient = new ContainerClient(containerUri);
    await containerClient.createIfNotExists();
    // Upload input data
     // Get input data blob Uri with SAS key
     const blobName = "input.json";
     const inputDataUri: string | undefined = (
       await quantumJobClient.storage.sasUri({
         containerName: storageContainerName,
         blobName: blobName
       })
     ).sasUri;

     if (inputDataUri === undefined){
        vscode.window.showWarningMessage(`Could not connect to storage blob`);
        return;
    }
     // Upload input data to blob
    const blobClient = new BlockBlobClient(inputDataUri);
    const problemFilename = await vscode.window.showInputBox({prompt:"Enter full file path",ignoreFocusOut:true});
    if (problemFilename === undefined){
        return;
    }

    const problemFileUri = vscode.Uri.file(problemFilename);
    const fileContent = await vscode.workspace.fs.readFile(problemFileUri);
    let fileConent: string| Uint8Array  = String.fromCharCode(...fileContent);


    const providerName = await vscode.window.showQuickPick(Object.keys(hardwareConfigs),{placeHolder:"Select Provider",ignoreFocusOut:true});
    if (!providerName){
        return;
    }
    const providerId = providerName.toLowerCase();

    const target = await vscode.window.showQuickPick((hardwareConfigs as { [key: string]: any })[providerName]["targets"],{placeHolder:"Select Target",ignoreFocusOut:true}) || "";
    const inputDataFormat = (hardwareConfigs as { [key: string]: any })[providerName]["inputDataFormat"];
    const outputDataFormat = (hardwareConfigs as { [key: string]: any })[providerName]["outputDataFormat"];
    let blobOptions = (hardwareConfigs as { [key: string]: any })[providerName]["blobOptions"];

    if (providerId ==='microsoft'){
        const utf8Data = unescape(encodeURIComponent(fileConent));
        fileConent = pako.gzip(utf8Data);
    }




    await blobClient.upload(fileConent, Buffer.byteLength(fileConent), blobOptions);
    const randomId = `${Math.floor(Math.random() * 10000 + 1)}`;
    // Submit job
    const jobId = `job-${randomId}`;
    const jobName = `jobName-${randomId}`;




    const createJobDetails = {
        containerUri: containerUri,
        inputDataFormat: inputDataFormat,
        providerId: providerId,
        target: target,
        id: jobId,
        inputDataUri: inputDataUri,
        name: jobName,
        outputDataFormat: outputDataFormat,
        inputParams:
        {
            params:
            {
                timeout:100,
                seed:22
            }
        }
    };
    await quantumJobClient.jobs.create(jobId, createJobDetails);
    vscode.window.showInformationMessage(jobId);
    const jobResult = await quantumJobClient.jobs.get(jobId);
    vscode.window.showInformationMessage(jobResult.status||"");
}


