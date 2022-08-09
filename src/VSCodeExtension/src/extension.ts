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
import { AzureCliCredential, InteractiveBrowserCredential, AccessToken } from '@azure/identity';
import {getWorkspaceFromUser} from "./quickPickWorkspace";
import {workspaceInfo, getAzureQuantumConfig, configIssueEnum} from "./commands";

let credential:AzureCliCredential|InteractiveBrowserCredential;
let authSource: string;
let accountAuthStatusBarItem: vscode.StatusBarItem;
let workSpaceStatusBarItem: vscode.StatusBarItem;
let submitJobStatusBarItem: vscode.StatusBarItem;

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

// required before any interaction with Azure
// try to authenticate though AzureCliCredential first. If fails, authenticate through InteractiveBrowserCredential
async function getCredential(context: vscode.ExtensionContext, changeAccount=false ){
    let token:AccessToken;
    if (credential && !changeAccount){
        return credential;
    }
        await vscode.window.withProgress({
        location: vscode.ProgressLocation.Notification,
        title: "Authenticating...",
        }, async (progress, token2) => {
    let tempCredential:any;
    try{
        tempCredential  = new AzureCliCredential();
        // if a user is changing their account always trigger InteractiveBrowserCredential
        if(changeAccount){
            // tslint:disable-next-line:no-unused-expression
            (token as any)["THROWERROR"];
        }
    // need to call getToken to validate if user is logged in through Az CLI
    token = await tempCredential.getToken(["https://management.azure.com/.default","https://quantum.microsoft.com/.default"]);
    authSource = "Az CLI";
    }
    catch{
        // login through browser if user is not logged in through Az CLI
        tempCredential = new InteractiveBrowserCredential();
        token = await tempCredential.getToken("https://management.azure.com/.default");
        authSource = "browser";
    }
    accountAuthStatusBarItem.tooltip = `Authenticated from ${authSource}`;
    accountAuthStatusBarItem.show();
    credential = tempCredential;
    });
    return;
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

    // need to call registerUIExtensionVariables to use openReadOnlyJson from
    // @microsoft/vscode-azext-utils package
    const AzExtOutputChannel = await createAzExtOutputChannel("trial", "quantum-devkit-vscode");
    const args: UIExtensionVariables = {context: context, outputChannel:AzExtOutputChannel};
    registerUIExtensionVariables(args);


    accountAuthStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 202);
    accountAuthStatusBarItem.command = "quantum.changeAzureAccount";
    context.subscriptions.push(accountAuthStatusBarItem);
    accountAuthStatusBarItem.text = `$(verified)`;

    if(authSource){
        accountAuthStatusBarItem.tooltip = `Azure Quantum Auth: ${authSource}`;
        accountAuthStatusBarItem.show();
    }

    submitJobStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 201);
    submitJobStatusBarItem.command = "quantum.submitJob";
    context.subscriptions.push(submitJobStatusBarItem);
    submitJobStatusBarItem.text = `$(run-above) Submit Job to Azure Quantum`;
    submitJobStatusBarItem.show();



    workSpaceStatusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 200);
    const workspaceInfo: workspaceInfo | undefined = context.workspaceState.get("workspaceInfo");
    workSpaceStatusBarItem.command = "quantum.changeWorkspace";
    context.subscriptions.push(workSpaceStatusBarItem);
    if (workspaceInfo && workspaceInfo["workspace"]){
        workSpaceStatusBarItem.text = `Azure Workspace: ${workspaceInfo["workspace"]}`;
        workSpaceStatusBarItem.show();
    }

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
//  Verify there are not nested csproj files
    function checkForNesting(files:any[]){
        // all csproj files must have same depth
        const depth = files[0].path.split("/").length;
        let file:any;
        let fileNum:any;
        for(fileNum in files){
            file = files[fileNum];
            if (file.path.split("/").length !== depth){
                return true;
            }
            return false;
        }


    }

    registerCommand(
        context,
        "quantum.submitJob",
        async() => {
            await getCredential(context);
            sendTelemetryEvent(EventNames.jobSubmissionStarted, {},{});
            // base amount of steps (provider, target, jobName, programArguments)
            let totalSteps = 4;
            // add steps if necessary for a user to connect to azure workspace
            let {workspaceInfo, configIssue} = await getAzureQuantumConfig();
            if(configIssue===configIssueEnum.MULTIPLE_CONFIGS){
              return;
            }
            totalSteps = configIssue?totalSteps+3:totalSteps;
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
                dotNetSdk => submitJob(context, dotNetSdk, localSubmissionsProvider, credential, workSpaceStatusBarItem, workspaceInfo, projectFiles, totalSteps)
            );
        }
    );

    // registerCommand(
    //     context,
    //     "quantum.disconnectFromAzureAccount",
    //     () => {
    //         deleteAzureWorkspaceInfo(context);
    //     }
    // );

    registerCommand(
        context,
        "quantum.changeWorkspace",
        async() => {
            sendTelemetryEvent(EventNames.changeWorkspace, {},{});
            await getCredential(context);
            // three total steps
            await getWorkspaceFromUser(context, credential, workSpaceStatusBarItem, 3);
        }
    );



    registerCommand(
        context,
        "quantum.getJob",
        async() => {
            sendTelemetryEvent(EventNames.getJobResults, {"method": "command line"},{});
            await getCredential(context);
            getJobResults(context, credential,workSpaceStatusBarItem);
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
        sendTelemetryEvent(EventNames.getJobDetails, {},{});
        await getCredential(context);
        const jobId = job['jobDetails']['jobId'];
        getJobDetails(context, credential, jobId, workSpaceStatusBarItem);
    });


    vscode.commands.registerCommand('quantum-jobs.jobResults', async (job) =>{
        await getCredential(context);
        const jobId = job['jobDetails']['jobId'];
        sendTelemetryEvent(
            EventNames.results,
            {
                'method': "Results button"
            },
        );
        getJobResults(context, credential,workSpaceStatusBarItem, jobId);
    }
    );
    vscode.commands.registerCommand('quantum.changeAzureAccount', async () =>{
        sendTelemetryEvent(EventNames.changeAzureAccount, {},{});
        await getCredential(context, true);
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
    if (reporter) { reporter.dispose(); }
}

