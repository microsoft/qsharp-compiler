// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

'use strict';
// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as vscode from 'vscode';
import { startTelemetry, EventNames, sendTelemetryEvent, reporter } from './telemetry';
import { DotnetInfo, requireDotNetSdk, findDotNetSdk } from './dotnet';
import { getPackageInfo } from './packageInfo';
import { installTemplates, createNewProject, registerCommand, openDocumentationHome, installOrUpdateIQSharp, submitJob, getJobResults, getJobDetails } from './commands';
import { LanguageServer } from './languageServer';
import {LocalSubmissionsProvider} from './localSubmissionsProvider';
import {registerUIExtensionVariables, createAzExtOutputChannel, UIExtensionVariables } from '@microsoft/vscode-azext-utils';
import { AzureCliCredential, InteractiveBrowserCredential, ChainedTokenCredential } from '@azure/identity';
import {getWorkspaceFromUser} from "./quickPickWorkspace";
import {workspaceInfo, getWorkspaceInfo} from "./commands";
import { AbortController} from "@azure/abort-controller";
import * as https from "https";

const findPort = require('find-open-port');
let credential:AzureCliCredential|InteractiveBrowserCredential;
let workspaceStatusBarItem: vscode.StatusBarItem;
let submitJobStatusBarItem: vscode.StatusBarItem;

// Flag to prevent user from launching multiple login flows
let currentlyAuthenticating = false;
// MSAL treats the Microsoft account system (Live, MSA) as another tenant
// within the Microsoft identity platform. The tenant id of the Microsoft
// account tenant is 9188040d-6c67-4c5b-b112-36a304b66dad
const MSA_ACCOUNT_TENANT = "9188040d-6c67-4c5b-b112-36a304b66dad";
/**
 * Returns the root folder for the current workspace.
 */
function findRootFolder() : string {
    // FIXME: handle multiple workspace folders here.
    let workspaceFolders = vscode.workspace.workspaceFolders;
    if (workspaceFolders) {
        return workspaceFolders[0].uri.fsPath;
    } else {
        return '';
    }
}

// handles cancellation of ports
async function abort(port:number, controller:AbortController){
    if (await findPort.isAvailable(port)){
      setTimeout(abort,100, port, controller);
    } else {
        controller.abort();
    }
  }



async function getCredential(context: vscode.ExtensionContext, changeAccountFlag=false ){
    return new Promise<void>(async(resolve, reject)=>{

    // For the general case where a user has already generated a credential
    // and is not changing accounts
    if (credential && !changeAccountFlag){
        return resolve();
    }

    // prevents user from having multiple authorization requests at the same time
    if(currentlyAuthenticating){
        return reject();
    }


        currentlyAuthenticating=true;
        const portOne = await findPort.findPort();
        let tempCredentialOne = new ChainedTokenCredential(new AzureCliCredential(), new InteractiveBrowserCredential({"redirectUri":`http://localhost:${portOne}`}));
        // Flag for triggering MSA authentication flow
        let authenticationMSAUser= false;
        // Below variables only needed for MSA user login
        let tempCredentialTwo: InteractiveBrowserCredential | undefined;
        let tenantJSON:any;
        let tenantId:string;


        await vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: "Authenticating...",
        "cancellable":true
        }, async (progress, cancel) => {
    try{
        const controllerOne = new AbortController();
        const abortSignalOne = controllerOne.signal;
        cancel.onCancellationRequested(async (e:any)=>{
            // abort the request, closing the tls port connection
            // otherwise the port could stay connected until the program
            // is terminated
            await abort(portOne, controllerOne);
            currentlyAuthenticating = false;
            return reject();

        });
        const token = await tempCredentialOne.getToken("https://management.azure.com/.default", {"abortSignal":abortSignalOne});

        //@ts-ignore
        authenticationMSAUser = !!(tempCredentialOne?._sources[1]?.msalFlow?.account?.homeAccountId.includes(MSA_ACCOUNT_TENANT));

        // Pull tenants ONLY for MSA users
        if(authenticationMSAUser && currentlyAuthenticating){
            const options:any = {
                headers: {
                  Authorization: `Bearer ${token.token}`,

                },
                resolveWithFullResponse:true
              };
            tenantJSON = await new Promise((resolve, reject)=>{

                //@ts-ignore
                const req = https.get("https://management.azure.com/tenants?api-version=2020-01-01", options, (res:any) => {
                    let responseBody = '';

                    res.on('data', (chunk:any) => {
                        responseBody += chunk;
                    });

                    res.on('end', () => {
                        resolve(JSON.parse(responseBody));
                    });
                });

                req.on('error', (err) => {
                    reject(err);
                });
                });

            }
        }
            catch(err:any){
                if(err && err.message === "Aborted"){
                    vscode.window.showErrorMessage("Unable to authenticate.");
                    }
                    currentlyAuthenticating=false;
                    return reject();
                }

            });
            // handles tenant selection for MSA users
            if(authenticationMSAUser && currentlyAuthenticating){
                if(tenantJSON.value.length === 0){
                    currentlyAuthenticating=false;
                    vscode.window.showErrorMessage("No tenants available.");
                    return reject();
                }
                else if (tenantJSON.value.length === 1){
                    tenantId = tenantJSON.value[0]["tenantId"];

                }
                else{
                    const tenantObj:any = await vscode.window.showQuickPick(tenantJSON.value.map((tenant:any)=>{
                        return {
                            label: tenant["displayName"],
                            description: tenant["tenantId"]
                            };
                        }),
                        {"ignoreFocusOut":true, "matchOnDescription":true, "title":"Select a Tenant."});

                    if(!tenantObj){
                        currentlyAuthenticating=false;
                        return reject();
                    }
                    tenantId = tenantObj["description"];

                }
                if (!tenantId){
                   currentlyAuthenticating=false;
                   return reject();
                }

            }

        // handles second authentication ONLY for MSA users
        if(authenticationMSAUser && currentlyAuthenticating){
        await vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: "Authenticating (there is a second browser login)...",
        "cancellable":true
        }, async (progress, cancel) => {
            const controllerTwo = new AbortController();
            const abortSignalTwo = controllerTwo.signal;
            try{
            const portTwo= await findPort.findPort();
            cancel.onCancellationRequested(async(e:any)=>{
            // abort the request, which opens the tls port for another authentication request
            await abort(portTwo, controllerTwo);
            currentlyAuthenticating = false;
            return reject();
            });

            tempCredentialTwo = new InteractiveBrowserCredential({"tenantId":tenantId, "redirectUri":`http://localhost:${portTwo}`});
            await tempCredentialTwo.getToken("https://management.azure.com/.default",  {"abortSignal":abortSignalTwo});

            // Verifies MSA user selected the same account in both authentication login pop-ups
            //@ts-ignore
            if(!!(tempCredentialOne?._sources[1]?.msalFlow?.account?.username !== tempCredentialTwo?.msalFlow?.account?.username)){
                throw Error("Aborted");
            }
            }
            catch(err:any){
                if(err && err.message === "Aborted"){
                    vscode.window.showErrorMessage("Unable to authenticate.");
                    }
                    currentlyAuthenticating=false;
                    return reject();
                }

            });
        }

        if (!currentlyAuthenticating){
            return reject();
        }

    const workspaceInfo: workspaceInfo | undefined = context.workspaceState.get("workspaceInfo");
    if (workspaceInfo && workspaceInfo["workspace"]){
        workspaceStatusBarItem.text = `Azure Workspace: ${workspaceInfo["workspace"]}`;
        workspaceStatusBarItem.show();
    }

    credential = tempCredentialTwo? tempCredentialTwo: tempCredentialOne;
    vscode.commands.executeCommand('setContext', 'showChangeAzureAccount', true);
    currentlyAuthenticating=false;
    return resolve();

    });

}

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export async function activate(context: vscode.ExtensionContext) {

    // Use the console to output diagnostic information (console.log) and errors (console.error)
    // This line of code will only be executed once when your extension is activated
    console.log('[qsharp-lsp] Activated!');
    process.env['VSCODE_LOG_LEVEL'] = 'trace';

    startTelemetry(context);


    let packageInfo = getPackageInfo(context);
    let dotNetSdkVersion = packageInfo === undefined ? undefined : packageInfo.requiredDotNetCoreSDK;


    // Get any .NET Core SDK version number to report in telemetry.
    var dotNetSdk : DotnetInfo | undefined;
    try {
        dotNetSdk = await findDotNetSdk();
    } catch {
        dotNetSdk = undefined;
    }



    sendTelemetryEvent(
        EventNames.activate,
        {
            'dotnetVersion': dotNetSdk !== undefined
                ? dotNetSdk.version
                : "<missing>"
        },
        {}
    );


    // creates treeview of locally submitted jobs in custom panel
    const localSubmissionsProvider = new LocalSubmissionsProvider(context);
    vscode.window.createTreeView("quantum-jobs",  {
        treeDataProvider: localSubmissionsProvider,
    });
    // shows "Connect to Azure Account" in command pallete, hides "Change Azure Account" from pallete
    vscode.commands.executeCommand('setContext', 'showChangeAzureAccount', false);

    // need to call registerUIExtensionVariables to use openReadOnlyJson from
    // @microsoft/vscode-azext-utils package
    const AzExtOutputChannel = await createAzExtOutputChannel("trial", "quantum-devkit-vscode");
    const args: UIExtensionVariables = {context: context, outputChannel:AzExtOutputChannel};
    registerUIExtensionVariables(args);

    context.workspaceState.update("workspaceInfo", undefined);

    submitJobStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 201);
    submitJobStatusBarItem.command = "quantum.submitJob";
    context.subscriptions.push(submitJobStatusBarItem);
    submitJobStatusBarItem.text = `$(run-above) Submit Job to Azure Quantum`;
    submitJobStatusBarItem.show();



    workspaceStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 200);
    if(!credential){

        workspaceStatusBarItem.text = "Connect to Azure Workspace";
        workspaceStatusBarItem.command = "quantum.getWorkspace";

        workspaceStatusBarItem.show();
    }
    context.subscriptions.push(workspaceStatusBarItem);


    // Register commands that use the .NET Core SDK.
    // We do so as early as possible so that we can handle if someone calls
    // a command before we found the .NET Core SDK.
    registerCommand(
        context,
        "quantum.newProject",
        () => {
            createNewProject(context);
        }
    );

    registerCommand(
        context,
        "quantum.installTemplates",
        () => {
            requireDotNetSdk(dotNetSdkVersion).then(
                dotNetSdk => installTemplates(dotNetSdk, packageInfo)
            );
        }
    );

    registerCommand(
        context,
        "quantum.openDocumentation",
        openDocumentationHome
    );

    registerCommand(
        context,
        "quantum.installIQSharp",
        () => {
            requireDotNetSdk(dotNetSdkVersion).then(
                dotNetSdk => installOrUpdateIQSharp(
                    dotNetSdk,
                    packageInfo ? packageInfo.nugetVersion : undefined
                )
            );
        }
    );

    registerCommand(
        context,
        "quantum.submitJob",
        async() => {
            await getCredential(context).then(()=>{
            requireDotNetSdk(dotNetSdkVersion).then(
                (dotNetSdk) => {
                sendTelemetryEvent(EventNames.jobSubmissionStarted, {},{});
                submitJob(context, dotNetSdk, localSubmissionsProvider, credential,workspaceStatusBarItem );
                }
            );
        }).catch((err)=>{
            if (err){
                console.log(err);
                }
        });
        }
    );


    registerCommand(
        context,
        "quantum.changeWorkspace",
        async() => {
        await getCredential(context).then(async()=>{
            await getWorkspaceFromUser(context, credential, workspaceStatusBarItem);
            sendTelemetryEvent(EventNames.changeWorkspace, {},{});
        }).catch((err)=>{
            if (err){
                console.log(err);
                }
        });
        }
    );

    registerCommand(
        context,
        "quantum.getWorkspace",
        async() => {
        await getCredential(context).then(async()=>{
            await getWorkspaceInfo(context, credential, workspaceStatusBarItem);
        }).catch((err)=>{
            if (err){
                console.log(err);
                }
        });
        }
    );



    registerCommand(
        context,
        "quantum.getJob",
        async() => {
            await getCredential(context).then(async()=>{
                await getJobResults(context, credential,workspaceStatusBarItem);
                sendTelemetryEvent(EventNames.getJobResults, {"method": "command line"},{});
            }).catch((err)=>{
                if (err){
                    console.log(err);
                    }
            });
        }
    );


    vscode.commands.registerCommand('quantum-jobs.clearJobs', async () =>{
        const userQuery = await vscode.window.showWarningMessage("Are you sure you want to clear your jobs?", ...["Clear","Cancel"]);
        if(userQuery === "Clear"){
            context.workspaceState.update("locallySubmittedJobs", undefined);
            localSubmissionsProvider.refresh(context);
        }
        }
    );

    vscode.commands.registerCommand('quantum-jobs.jobDetails', async (job) =>{
        await getCredential(context).then(async()=>{
            const jobId = job['jobDetails']['jobId'];
            await getJobDetails(context, credential,workspaceStatusBarItem, jobId);
            sendTelemetryEvent(EventNames.getJobDetails, {},{});
        }).catch((err)=>{
            if (err){
                console.log(err);
                }
        });
    });


    vscode.commands.registerCommand('quantum-jobs.jobResults', async (job) =>{
        await getCredential(context).then(async()=>{
            sendTelemetryEvent(
                EventNames.results,
                {
                    'method': "Results button"
                },
            );
            const jobId = job['jobDetails']['jobId'];
            await getJobResults(context, credential,workspaceStatusBarItem, jobId);
        }).catch((err)=>{
            if (err){
                console.log(err);
                }
        });
    }
    );
    vscode.commands.registerCommand('quantum.connectToAzureAccount', async () =>{
        sendTelemetryEvent(EventNames.connectToAzureAccount, {},{});
        await getCredential(context);
    });

    vscode.commands.registerCommand('quantum.changeAzureAccount', async () =>{
        await getCredential(context, true).then(async()=>{
            sendTelemetryEvent(EventNames.changeAzureAccount, {},{});
            context.workspaceState.update("workspaceInfo", undefined);
        }).catch((err)=>{
            if (err){
                console.log(err);
                }
        });
    });



    let rootFolder = findRootFolder();

    // Start the language server client.
    let languageServer = new LanguageServer(context, rootFolder);
    await languageServer
        .start()
        .catch(
            err => {
                console.log(`[qsharp-lsp] Language server failed to start: ${err}`);
                let reportFeedbackItem = "Report feedback...";
                vscode.window.showErrorMessage(
                    `Language server failed to start: ${err}`,
                    reportFeedbackItem
                ).then(
                    item => {
                        vscode.env.openExternal(vscode.Uri.parse(
                            "https://github.com/microsoft/qsharp-compiler/issues/new?assignees=&labels=bug,Area-IDE&template=bug_report.md&title="
                        ));
                    }
                );
            }
        );

        return context;

}

// this method is called when your extension is deactivated
export function deactivate() {
    currentlyAuthenticating = false;
    if (reporter) { reporter.dispose(); }
}

