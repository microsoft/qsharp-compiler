// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
import * as vscode from 'vscode';
import * as cp from 'child_process';
import * as https from 'https';

import { DefaultAzureCredential } from "@azure/identity";
import { QuantumJobClient } from "@azure/quantum-jobs";


import { DotnetInfo, findIQSharpVersion } from './dotnet';
import { IPackageInfo } from './packageInfo';
import * as semver from 'semver';
import { promisify } from 'util';
import { QSharpGenerator } from './yeoman-generator';
import {LocalSubmissionItem, LocalSubmissionsProvider} from './localSubmissionsProvider';

import * as excludedProviders from "./utils/excludedProviders.json";

import * as yeoman from 'yeoman-environment';
import {openReadOnlyJson } from '@microsoft/vscode-azext-utils';


type accountInfo = {
    subscriptionId: string;
    resourceGroupName: string;
    workspaceName: string,
    location: string
  };

type target = {
    id: string,
    currentAvailability:string,
    averageQueueTime: number,
    statusPage: string
};

export function registerCommand(context: vscode.ExtensionContext, name: string, action: () => void) {
    context.subscriptions.push(
        vscode.commands.registerCommand(
            name,
            () => {
                action();
            }
        )
    );
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
    );

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

export async function storeAzureAccount(context: vscode.ExtensionContext) {
    const subscriptionId = await vscode.window.showInputBox({prompt:"Enter subscription ID",ignoreFocusOut:true});
    const resourceGroupName = subscriptionId && await vscode.window.showInputBox({prompt:"Enter Resource Group Name",ignoreFocusOut:true});
    const workspaceName = resourceGroupName && await vscode.window.showInputBox({prompt:"Enter Workspace Name",ignoreFocusOut:true});
    const location = workspaceName && await vscode.window.showInputBox({prompt:"Enter location",ignoreFocusOut:true});

    // do not update accountInfo if any of the fields are empty
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
export async function deleteAzureAccountInfo(context: vscode.ExtensionContext) {
    context.workspaceState.update("accountInfo", undefined);
    vscode.window.showInformationMessage("Successfully Disconnected");
}

function getQuantumJobClient(accountInfo:accountInfo, credential:DefaultAzureCredential){
    const endpoint = "https://" + accountInfo["location"] + ".quantum.azure.com";
    return new QuantumJobClient(
        credential,
        accountInfo["subscriptionId"],
        accountInfo["resourceGroupName"],
        accountInfo["workspaceName"],
        {
        endpoint: endpoint,
        credentialScopes: "https://quantum.microsoft.com/.default"
        }
    );

}

export async function submitJob(context: vscode.ExtensionContext, dotNetSdk: DotnetInfo, jobsSubmittedProvider: LocalSubmissionsProvider) {
    let accountInfo: accountInfo | undefined = context.workspaceState.get("accountInfo");

    if (accountInfo === undefined){
        await storeAzureAccount(context);
        accountInfo = context.workspaceState.get("accountInfo");
    }
    // TODO ADD FUNCTIONALITY TO ALLOW USER TO RETRY LOGIN STEP
    if (accountInfo ===undefined){
        vscode.window.showWarningMessage(`Could not connect to Azure Account`);
        return;
    }

    const credential = new DefaultAzureCredential();
    const quantumJobClient = getQuantumJobClient(accountInfo, credential);


    // find project file
    const projectFile = await vscode.workspace.findFiles('**/*.csproj');

    // get project file path
    let projectFilePath = (projectFile && projectFile[0] && projectFile[0]['path'])? projectFile[0]['path']: undefined;
    if (!projectFilePath){
        vscode.window.showWarningMessage(`Could not find .csproj`);
        return;
    }
    projectFilePath = projectFilePath.replace(/[/]/g,"\\").substring(1);

    // retrieve targets available in workspace
    const availableTargets: { [key: string]: any } = {};
    const providerStatuses = await quantumJobClient.providers.listStatus();
    let iter;
    while(iter = await providerStatuses.next()){
        if (iter && !iter.value){
            break;
        }
    // For now, only concern is .qs programs, not json optimization programs
    // so filter out optimization providers
        if (excludedProviders["optimizationProviders"].includes( iter.value.id)){
            continue;
        }

        const provider = iter['value']['id'];
        const targets = iter['value']['targets'];
        availableTargets[provider] = targets;
    }

    // job name not required
    const jobName = await vscode.window.showInputBox({placeHolder:"Enter a name for the job",ignoreFocusOut:true, title:"Job Name"});
    const providerName = await vscode.window.showQuickPick(Object.keys(availableTargets),{placeHolder:"Select Provider",ignoreFocusOut:true});
    if (!providerName){
        return;
    }


    const target = await vscode.window.showQuickPick((availableTargets as { [key: string]: any })[providerName].map((t:target)=>t['id']),{placeHolder:"Select Target",ignoreFocusOut:true});
    if(!target){
        return;
    }

    const additionalArgs =  await vscode.window.showInputBox({placeHolder:"--n-qubits=2",ignoreFocusOut:true, title:"Additional Arguments"});
    // if user does not provide job name, generate one
    const randomId = `${Math.floor(Math.random() * 10000 + 1)}`;
    const alternateJobName = `jobName-${randomId}`;

    const token = await credential.getToken('https://quantum.microsoft.com');

    await promisify(cp.execFile)(`${dotNetSdk.path}`, ["build",`${projectFilePath}`, `-property:ExecutionTarget=${target}`])
    .then(edit => {
        if (!edit || !accountInfo){
            throw Error;
        }


    let args = ["dotnet", "run", "--no-build"];

    args.push("--project");
    args.push(projectFilePath  as string);

    args.push("--");
    args.push("submit");

    args.push("--subscription");
    args.push(accountInfo["subscriptionId"]);

    args.push("--resource-group");
    args.push(accountInfo["resourceGroupName"]);

    args.push("--workspace");
    args.push(accountInfo["workspaceName"]);

    args.push("--target");
    args.push(target);

    args.push("--output");
    args.push("Id");

    args.push("--job-name");
    args.push(jobName||alternateJobName);

    args.push("--aad-token");
    args.push(token.token);

    args.push("--location");
    args.push(accountInfo["location"]);

    args.push("--user-agent");
    args.push("VSCODE");

    if(additionalArgs){
        args = args.concat(additionalArgs.split(" "));
    }

    promisify(cp.execFile)(`${dotNetSdk.path}`, args)
    .then(job => {
        if (!job || !accountInfo){
            throw Error;
        }
        // job.stdout returns job id with an unnecessary space and new line char
        const jobId =job.stdout.slice(0,-2);
        vscode.window.showInformationMessage(`Job Id: ${jobId}`);

        const timeStamp = new Date().toLocaleString('en',
            {day:"2-digit",
            month: "2-digit",
            year: "2-digit",
            hour:"2-digit",
            minute:"2-digit"
        });

        const jobDetails = {
            "Date": timeStamp,
            "Subscription Id":accountInfo["subscriptionId"],
            "Resource Group":accountInfo["resourceGroupName"],
            "Workspace": accountInfo["workspaceName"],
            "Location": accountInfo["location"],
            "Job Id": jobId,
            "Job Name": jobName? jobName: "NA",
            "Provider": providerName,
            "Target":target,
            "Additional Args":  additionalArgs?additionalArgs:"NA"
        };

        // Add job to the extension's panel
        const locallySubmittedJobs: any = context.workspaceState.get("locallySubmittedJobs");
        const updatedSubmittedJobIds = locallySubmittedJobs?[jobDetails, ...locallySubmittedJobs]: [jobDetails];
        context.workspaceState.update("locallySubmittedJobs",updatedSubmittedJobIds);
        jobsSubmittedProvider.refresh(context);
        vscode.commands.executeCommand("quantum-jobs.focus");

        }).then(undefined, err => {
            console.error('I am a runtime error ');
            });

    }).then(undefined, err => {
       console.error('I am a compilation error');
    });

}

// getJobResults helper function
function outputResults(context: vscode.ExtensionContext, jobName: string, jobId: string, results: string){
    let outputChannel = context.workspaceState.get("jobOutputChannel")  as vscode.OutputChannel;
    if (!outputChannel || !outputChannel["appendLine"]){
        outputChannel = vscode.window.createOutputChannel("Quantum Job Results");
        context.workspaceState.update("jobOutputChannel", outputChannel);
    }

    outputChannel.appendLine(`Results for job ${jobName}(${jobId}):`);
    outputChannel.appendLine(results);
    outputChannel.show();
}

// if users requests job results from the Command Palette, jobId param will be undefined
// if users requests job results from the panel, jobId will be passed
export async function getJobResults(context: vscode.ExtensionContext, jobId?: string) {
    let accountInfo: accountInfo | undefined = context.workspaceState.get("accountInfo");
    if (accountInfo ===undefined){
        vscode.window.showWarningMessage(`Could not connect to Azure Account`);
        return;
    }

    // inputBox for a user calling from Command Palette
    if (!jobId){
        jobId = await vscode.window.showInputBox({prompt:"Enter Job Id",ignoreFocusOut:true});
    }
    if (!jobId){
        return;
    }
    // caches job results, so if user requests the same job results they can avoid load time
    let cachedJobs = context.workspaceState.get("cachedJobs") as any;


    if (cachedJobs && cachedJobs[jobId]){
        outputResults(context, cachedJobs[jobId]["jobName"], jobId, cachedJobs[jobId]["result"]);
        return;
    }

    if (!cachedJobs){
        cachedJobs = {};
    }

    const credential = new DefaultAzureCredential();
    const quantumJobClient = getQuantumJobClient(accountInfo, credential);

    const jobResponse = await quantumJobClient.jobs.get(jobId);

      https.get(jobResponse.outputDataUri||"", function(response){
        response.on('data', (chunk : number[]) => {
            const outputString = String.fromCharCode(...chunk) +"\n";
            outputResults(context, jobResponse.name as string, jobId as string, outputString);
            // cache job results
            if (!outputString.includes("Error")){
                const cacheDetails = {"jobName":jobResponse.name,"result":outputString};
                cachedJobs[jobId as string] = cacheDetails;
                context.workspaceState.update("cachedJobs", cachedJobs);
            }
        });

      }).on('error', function(err){
        console.log(err);
      });

}



export async function getJobDetails(context: vscode.ExtensionContext, node: LocalSubmissionItem): Promise<void> {
    await openReadOnlyJson({'label': node.jobDetails["Job Id"], 'fullId':node.jobDetails["Job Id"]}, node.jobDetails);
}





