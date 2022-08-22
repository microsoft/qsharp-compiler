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
import {getConfig, setWorkspace} from "./configFileHelpers";
import { AbortController} from "@azure/abort-controller";
import * as https from "https";
import {MSA_ACCOUNT_TENANT, workspaceStatusEnum} from "./utils/constants";
import {checkForNesting} from "./checkForNesting";
import {setupDefaultWorkspaceStatusButton, setupUnknownWorkspaceStatusButton} from "./workspaceStatusButtonHelpers";
const findPort = require('find-open-port');

// credential used throughout application
let credential:AzureCliCredential|InteractiveBrowserCredential;
let workspaceStatusBarItem: vscode.StatusBarItem;
let submitJobStatusBarItem: vscode.StatusBarItem;

// Flag to prevent user from launching multiple login flows
let currentlyAuthenticating = false;


// handles cancellation of ports
async function abort(port:number, controller:AbortController){
    if (await findPort.isAvailable(port)){
      setTimeout(abort,100, port, controller);
    } else {
        controller.abort();
    }
  }

// Authentication has three flows depending on 1) is user logged in
// through Az Cli. If not, is user an 2) aad user or 3) a MSA user?
// Flow #1, user does not have to sign into any pop-up browsers. They 
// are already autenticated
// Flow #2, user has to sign into one pop-up browser and is logged in
// Flow #3, user has to sign into two pop-up browsers and select tenant
// if they are under multiple tenants
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
        // find an open port
        const portOne = await findPort.findPort();
        // Flag for triggering MSA authentication flow
        let authenticationMSAUser= false;
        let tempCredentialOne: any;
        let msalFlowInfo:any;
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
        // if user is changing account do not take auto log in from Az Cli
        if(changeAccountFlag){
            tempCredentialOne = new  InteractiveBrowserCredential({"redirectUri":`http://localhost:${portOne}`});
        }
        else{
            tempCredentialOne = new ChainedTokenCredential(new AzureCliCredential(), new InteractiveBrowserCredential({"redirectUri":`http://localhost:${portOne}`}));
        }
        // abort controllers needed as unless the browser login is
        // successful InteractiveBrowserCredential doesn't close
        // the port connection
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

        // all MSA users have the MSA_ACCOUNT_TENANT Id in their home account id. AAD users do not.
        //@ts-ignore
        if(changeAccountFlag){
            authenticationMSAUser = !!(tempCredentialOne?.msalFlow?.account?.homeAccountId?.includes(MSA_ACCOUNT_TENANT));
            msalFlowInfo = tempCredentialOne?.msalFlow;
        }
        else{
            // For chainedCredential_sources[0] is Az Cli credential, _sources[1] is interactive browser credential
            authenticationMSAUser = !!(tempCredentialOne?._sources[1]?.msalFlow?.account?.homeAccountId?.includes(MSA_ACCOUNT_TENANT));
            msalFlowInfo = tempCredentialOne?._sources[1]?.msalFlow;
        }
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
                if(err?.message === "Aborted"){
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
                // if one tenant, auto select
                else if (tenantJSON.value.length === 1){
                    tenantId = tenantJSON.value[0]["tenantId"];

                }
                // if multiple tenants, show quickpick to select one
                else{
                    tenantJSON.value.sort(function(tenant1:any, tenant2:any) {
                        return tenant1.displayName.localeCompare(tenant2.displayName);
                      });
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
            // can't use first port as it could still be closing
            // the connection from the previous authentication
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
            if(!!(msalFlowInfo?.account?.username !== tempCredentialTwo?.msalFlow?.account?.username)){
                throw Error("Aborted");
            }
            }
            catch(err:any){
                if(err?.message === "Aborted"){
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

    credential = tempCredentialTwo? tempCredentialTwo: tempCredentialOne;
    // Shows "Q#: Change Azure Account" and "Q#: Set Azure Workspace" in
    // palette and removes "Q#: Connect to Azure Account"
    vscode.commands.executeCommand('setContext', 'showChangeAzureAccount', true);
    currentlyAuthenticating=false;
    return resolve();
    });
}

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

    // Shows "Connect to Azure Account" in command pallete, hides 
    // "Change Azure Account" from pallete
    vscode.commands.executeCommand('setContext', 'showChangeAzureAccount', false);

    // Creates treeview of locally submitted jobs in custom extension panel
    const localSubmissionsProvider = new LocalSubmissionsProvider(context);
    vscode.window.createTreeView("quantum-jobs",  {
        treeDataProvider: localSubmissionsProvider,
    });
    // Need to call registerUIExtensionVariables to use openReadOnlyJson from
    // @microsoft/vscode-azext-utils package
    const AzExtOutputChannel = await createAzExtOutputChannel("trial", "quantum-devkit-vscode");
    const args: UIExtensionVariables = {context: context, outputChannel:AzExtOutputChannel};
    registerUIExtensionVariables(args);

    // Create Submit Job status bar button
    submitJobStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 201);
    submitJobStatusBarItem.command = "quantum.submitJob";
    context.subscriptions.push(submitJobStatusBarItem);
    submitJobStatusBarItem.text = `$(run-above) Submit Job to Azure Quantum`;
    submitJobStatusBarItem.show();

    // Create Workspace status bar button
    // Starts in first state "Connect to Azure Account"
    workspaceStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 200);
    // There is no workspace set on intialization, so set workspace wide variable as unknown.
    if(!credential){
        setupDefaultWorkspaceStatusButton(context, workspaceStatusBarItem);
    }
    context.subscriptions.push(workspaceStatusBarItem);


    // When a user has made a change to the azurequantumconfig.json, 
    // set workspace status button to unknown state. To verify, the
    //  user must run any command or click the status bar button.
    vscode.workspace.onDidSaveTextDocument(async(doc:any)=>{
        if(doc?.fileName?.split("/")?.pop() === "azurequantumconfig.json"){
            await setupUnknownWorkspaceStatusButton(context, credential, workspaceStatusBarItem);
        }

    });

    // When a user has deleted the azurequantumconfig.json, set workspace 
    // status button to default state ("Connect to Azure Quantum")
    vscode.workspace.onDidDeleteFiles((files:any)=>{
        const numOfFiles = files?.files?.length || 0;
        for (let i =0; i<numOfFiles;i++){
            if(files?.files[i]?.path?.split("/")?.pop() === "azurequantumconfig.json"){
                setupDefaultWorkspaceStatusButton(context, workspaceStatusBarItem);
            }
        }
    });


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


    vscode.commands.registerCommand('quantum.connectToAzureAccount', async () =>{
        sendTelemetryEvent(EventNames.connectToAzureAccount, {},{});
        await getCredential(context).then(async()=>{
            sendTelemetryEvent(EventNames.connectToAzureAccount, {},{});
            vscode.window.showInformationMessage("Successfully connected to account.");

        }).catch((err)=>{
            if (err){
                sendTelemetryEvent(EventNames.error, {
                    error: err
                });
                console.log(err);
            }
        });
    });

    vscode.commands.registerCommand('quantum.changeAzureAccount', async () =>{
        await getCredential(context, true).then(async()=>{
            sendTelemetryEvent(EventNames.changeAzureAccount, {},{});
            // clear local jobs panel to avoid authorization issues for user
            context.workspaceState.update("locallySubmittedJobs", undefined);
            localSubmissionsProvider.refresh(context);
            vscode.window.showInformationMessage("Successfully connected to account.");
            await setupUnknownWorkspaceStatusButton(context, credential, workspaceStatusBarItem);
        }).catch((err)=>{
            if (err){
                sendTelemetryEvent(EventNames.error, {
                    error: err
                });
                console.log(err);
            }
        });
    });


    registerCommand(
        context,
        "quantum.changeWorkspace",
        async() => {
        await getCredential(context).then(async()=>{
            // Get workspace status prior to change, as local submitted
            // jobs panel is only cleared when a user is in an Authorized
            // workspace. We do not clear the local jobs panel if the
            // user's workspace status is unknown or unauthorized
            const oldStatus = context.workspaceState.get("workspaceStatus");
            // Get current workspace if available to avoid clearing
            // local jobs submission panel if a user selects same workspace
            // they are currently in. Pass false for validation flag as 
            // the user is in process of changing workspace and therefore
            // does not need to be shown the error message that they are 
            // in an unauthorized workspace.
            let {workspaceInfo:oldWorkspaceInfo} = await getConfig(context, credential, workspaceStatusBarItem, false);
            const newWorkspaceInfo = await getWorkspaceFromUser(context, credential, workspaceStatusBarItem, 3, oldWorkspaceInfo);
            sendTelemetryEvent(EventNames.changeWorkspace, {},{});
            // Only clear local jobs is user changes workspaces and has
            // a currently authorized workspace status
            if((newWorkspaceInfo?.workspace!==oldWorkspaceInfo?.workspace)&& oldStatus === workspaceStatusEnum.AUTHORIZED){
            context.workspaceState.update("locallySubmittedJobs", undefined);
            localSubmissionsProvider.refresh(context);
            }
        }).catch((err)=>{
            if (err){
                sendTelemetryEvent(EventNames.error, {
                    error: err
                });
                console.log(err);
                }
        });
        }
    );


    // If config not present, queries user for workspace information
    // If config present, verifies config. If verification fails, 
    // does NOT automatically query user for workspace information
    registerCommand(
        context,
        "quantum.getWorkspace",
        async() => {
        await getCredential(context).then(async()=>{
        await setWorkspace(context, credential, workspaceStatusBarItem, 0);
        }).catch((err)=>{
            if (err){
                sendTelemetryEvent(EventNames.error, {
                    error: err
                });
                console.log(err);
                }
        });

        }
    );
 

    registerCommand(
        context,
        "quantum.submitJob",
        async() => {
            await getCredential(context).then( async()=>{
            sendTelemetryEvent(EventNames.jobSubmissionStarted, {},{});
            // base amount of steps (provider, target, jobName, programArguments)
            let totalSteps = 4;
            // add steps if necessary for a user to connect to azure workspace
            let {workspaceInfo, exitRequest} = await getConfig(context, credential, workspaceStatusBarItem);
            if (exitRequest){
                return;
            }
            totalSteps = !workspaceInfo?totalSteps+3:totalSteps;
            // find project file
            const projectFiles = await vscode.workspace.findFiles("**/*.csproj");
            if(!projectFiles || projectFiles.length === 0){
                vscode.window.showErrorMessage(`Could not find .csproj`);
                return;
            }
            const multipleProjFilesFlag = projectFiles.length>1?true:false;
            if(multipleProjFilesFlag && checkForNesting(projectFiles)){
                vscode.window.showErrorMessage("Nested .csproj files is not currently supported.");
                return;
            }
            // add step if there are multiple .csproj and user needs to select one
            totalSteps = multipleProjFilesFlag?totalSteps+1:totalSteps;

            requireDotNetSdk(dotNetSdkVersion).then(
                dotNetSdk => submitJob(context, dotNetSdk, localSubmissionsProvider, credential, workspaceStatusBarItem, workspaceInfo, projectFiles, totalSteps)
            );
        }).catch((err)=>{
            if (err){
                sendTelemetryEvent(EventNames.error, {
                    error: err
                });
                console.log(err);
                }
        });
        }
    );

    registerCommand(
        context,
        "quantum.jobResultsPalette",
        async() => {
            await getCredential(context).then(async()=>{
                // if a user does not have to enter workspace info
                // total steps will not be specified as the only step 
                // is for a user to enter in a job id
                let totalSteps:number|undefined = undefined;
                // add steps if necessary for a user to connect to azure workspace
                let {workspaceInfo, exitRequest} = await getConfig(context, credential, workspaceStatusBarItem);
                if(exitRequest){
                  return;
                }
                if(!workspaceInfo){
                    totalSteps = 4;
                    workspaceInfo = await getWorkspaceFromUser(context, credential, workspaceStatusBarItem, totalSteps);
                }
                if(!workspaceInfo){
                    return; 
                }
                await getJobResults(credential,workspaceInfo, totalSteps);
                sendTelemetryEvent(EventNames.getJobResults, {"method": "command line"},{});
            }).catch((err)=>{
                if (err){
                    sendTelemetryEvent(EventNames.error, {
                        error: err
                    });
                    console.log(err);
                    }
            });
        }
    );

    vscode.commands.registerCommand('quantum-jobs.jobResultsButton', async (job) =>{
        await getCredential(context).then(async()=>{
            const jobId:string = job['jobDetails']['jobId'];
            // There are no additional steps as specifieid by the 0 in 
            // the call below. The job id populates from the treeview. 
            const workspaceInfo = await setWorkspace(context, credential, workspaceStatusBarItem, 0);
            if(!workspaceInfo){
                return;
            }
            await getJobResults(credential,workspaceInfo, undefined, jobId);
            sendTelemetryEvent(
                EventNames.results,
                {
                    'method': "Results button"
                },
            );
        }).catch((err)=>{
            if (err){
                sendTelemetryEvent(EventNames.error, {
                    error: err
                });
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
    });

    vscode.commands.registerCommand('quantum-jobs.jobDetails', async (job) =>{
        await getCredential(context).then(async()=>{
            const jobId:string = job['jobDetails']['jobId'];
            // There are no additional steps as specifieid by the 0 in 
            // the call below. The job id populates from the treeview. 
            const workspaceInfo = await setWorkspace(context, credential, workspaceStatusBarItem, 0);
            if(!workspaceInfo){
                return;
            }
            await getJobDetails(credential,workspaceInfo, jobId);
            sendTelemetryEvent(EventNames.getJobDetails, {},{});
        }).catch((err)=>{
            if (err){
                sendTelemetryEvent(EventNames.error, {
                    error: err
                });
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

