// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

"use strict";
import * as vscode from "vscode";
import * as cp from "child_process";
import * as https from "https";
import {
  AzureCliCredential,
  InteractiveBrowserCredential,
} from "@azure/identity";
import { QuantumJobClient } from "@azure/quantum-jobs";
import { getPackageInfo } from './packageInfo';

import { DotnetInfo, findIQSharpVersion } from "./dotnet";
import { IPackageInfo } from "./packageInfo";
import * as semver from "semver";
import { promisify } from "util";
import { QSharpGenerator } from "./yeoman-generator";
import {
  LocalSubmissionsProvider,
} from "./localSubmissionsProvider";

import * as yeoman from "yeoman-environment";
import { openReadOnlyJson } from "@microsoft/vscode-azext-utils";
import {getWorkspaceFromUser} from "./quickPickWorkspace";
import {getJobInfoFromUser} from "./quickPickJob";

export type workspaceInfo = {
  subscriptionId: string;
  resourceGroup: string;
  workspace: string;
  location: string;
};

export function registerCommand(
  context: vscode.ExtensionContext,
  name: string,
  action: () => void
) {
  context.subscriptions.push(
    vscode.commands.registerCommand(name, () => {
      action();
    })
  );
}

export async function createNewProject(context: vscode.ExtensionContext) {
  let env = yeoman.createEnv();
  env.registerStub(QSharpGenerator, "qsharp:app");
  // Disable type checking on the env object at this point due to
  // https://github.com/yeoman/environment/issues/273.
  let anyEnv = env as any;
  let err = await anyEnv.run("qsharp:app", {
    extensionPath: context.extensionPath,
  });
  if (err) {
    let errorMessage = err.name + ": " + err.message;
    console.log(errorMessage);
    vscode.window.showErrorMessage(errorMessage);
  }
}

export function installTemplates(
  dotNetSdk: DotnetInfo,
  packageInfo?: IPackageInfo
) {
  let packageVersion = packageInfo?.nugetVersion
    ? `::${packageInfo!.nugetVersion}`
    : "";
  let proc = cp.spawn(dotNetSdk.path, [
    "new",
    "--install",
    `Microsoft.Quantum.ProjectTemplates${packageVersion}`,
  ]);

  let errorMessage = "";
  proc.stderr.on("data", (data) => {
    errorMessage = errorMessage + data;
  });
  proc.stdout.on("data", (data) => {
    console.log("" + data);
  });

  proc.on("exit", (code, signal) => {
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
  });
}

export function openDocumentationHome() {
  return vscode.env.openExternal(
    vscode.Uri.parse("https://docs.microsoft.com/azure/quantum/")
  );
}

export function installOrUpdateIQSharp(
  dotNetSdk: DotnetInfo,
  requiredVersion?: string
) {
  findIQSharpVersion()
    .then((iqsharpVersion) => {
      if (
        iqsharpVersion !== undefined &&
        iqsharpVersion["iqsharp"] !== undefined
      ) {
        // We got a version, so let's check if it's up to date or not.
        // If it is up to date, we print out that this is the case and resolve the
        // promise immediately.
        if (
          requiredVersion === undefined ||
          semver.gte(iqsharpVersion["iqsharp"], requiredVersion)
        ) {
          vscode.window.showInformationMessage(
            `Currently IQ# version is up to date (${iqsharpVersion["iqsharp"]}).`
          );
          return false;
        }

        // If we made it here, we need to install IQ#. This can fail if it's already installed.
        // While dotnet does offer an update command, it's often more reliable to just uninstall and reinstall.
        // Thus, we uninstall here before proceeding to the install step below.
        return promisify(cp.exec)(
          `"${dotNetSdk.path}" tool uninstall --global Microsoft.Quantum.IQSharp`
        ).then(() => true);
      }
      return true;
    })
    .then((needToInstall) => {
      if (needToInstall) {
        let versionSpec =
          requiredVersion === undefined ? "" : `::${requiredVersion}`;
        return promisify(cp.exec)(
          `"${dotNetSdk.path}" tool install --global Microsoft.Quantum.IQSharp${versionSpec}`
        ).then(() => {
          // Check what version actually got installed and report that back.
          findIQSharpVersion().then((installedVersion) => {
            if (installedVersion === undefined) {
              throw new Error("Could not detect IQ# version after installing.");
            }
            if (installedVersion["iqsharp"] === undefined) {
              throw new Error("Newly installed IQ# did not report a version.");
            }
            vscode.window.showInformationMessage(
              `Successfully installed IQ# version ${installedVersion["iqsharp"]}`
            );
          });
        });
      }
    })
    .catch((reason) => {
      vscode.window.showWarningMessage(`Could not install IQ#:\n${reason}`);
    });
}

// try to pull workspace from config file. If nonexistent or incorrect format prompt user for workspace info.
async function getWorkspaceInfo(context: vscode.ExtensionContext, credential:InteractiveBrowserCredential | AzureCliCredential, workSpaceStatusBarItem: vscode.StatusBarItem, submitJobCommand=false){
    let workspaceInfo:any;
    let userInputNeeded=false;
    const configFile = await vscode.workspace.findFiles(
        "**/azurequantumconfig.json"
      );

      if (configFile && configFile[0]) {
        const workspaceInfoChunk: any = await vscode.workspace.fs.readFile(
          configFile[0]
        );
        try{
        workspaceInfo = JSON.parse(String.fromCharCode(...workspaceInfoChunk));
        if (!workspaceInfo["subscriptionId"]||!workspaceInfo["resourceGroup"]||!workspaceInfo["workspace"]||!workspaceInfo["location"]){
            throw Error;
        }
    }
        catch{
            workspaceInfo = undefined;
            vscode.window.showWarningMessage("Invalid azurequantumconfig.json format");
        }
      }

      if (workspaceInfo === undefined) {
        // remove any previous account info
        context.workspaceState.update("workspaceInfo", undefined);
        try{
        userInputNeeded = true;
        // if this function is called from job submission command, submitJobCommand flag makes
        // total steps 7 instead of 3 for the workspace entries required.
        await getWorkspaceFromUser(context, credential, workSpaceStatusBarItem, submitJobCommand);
        workspaceInfo = context.workspaceState.get("workspaceInfo");
        }
        catch{
            vscode.window.showWarningMessage(`Could not connect to Azure Account`);
            return;
        }
      }

      if (workspaceInfo === undefined) {
        vscode.window.showWarningMessage(`Could not connect to Azure Account`);
        return;
      }
      // if this function is called from job submission command, userInputNeeded flag makes
      // total steps 7 instead of 4 for the job entries required.
      return [workspaceInfo,userInputNeeded];
}








export async function deleteAzureWorkspaceInfo(context: vscode.ExtensionContext) {
  context.workspaceState.update("workspaceInfo", undefined);
  vscode.window.showInformationMessage("Successfully Disconnected");
}


function getQuantumJobClient(workspaceInfo: workspaceInfo, credential: any) {
  const endpoint = "https://" + workspaceInfo["location"] + ".quantum.azure.com";
  return new QuantumJobClient(
    credential,
    workspaceInfo["subscriptionId"],
    workspaceInfo["resourceGroup"],
    workspaceInfo["workspace"],
    {
      endpoint: endpoint,
      credentialScopes: "https://quantum.microsoft.com/.default",
    }
  );
}

// submitting program to azure quantum
export async function submitJob(
  context: vscode.ExtensionContext,
  dotNetSdk: DotnetInfo,
  jobsSubmittedProvider: LocalSubmissionsProvider,
  credential: InteractiveBrowserCredential | AzureCliCredential,
  workSpaceStatusBarItem: vscode.StatusBarItem
) {

    const workspaceFlow = await getWorkspaceInfo(context, credential,workSpaceStatusBarItem, true);
    if (!workspaceFlow){
        return;
      }

  let workspaceInfo = workspaceFlow[0];

  const quantumJobClient = getQuantumJobClient(workspaceInfo, credential);

  // find project file
  const projectFile = await vscode.workspace.findFiles("**/*.csproj");

  // get project file path
  let projectFilePath =
    projectFile && projectFile[0] && projectFile[0]["path"]
      ? projectFile[0]["path"]
      : undefined;
  if (!projectFilePath) {
    vscode.window.showWarningMessage(`Could not find .csproj`);
    return;
  }
  projectFilePath = projectFilePath.replace(/[/]/g, "\\").substring(1);


  const {jobName, provider, target, programArguments } = await getJobInfoFromUser(context,quantumJobClient, workspaceInfo, workspaceFlow[1]);

  vscode.window.withProgress(
    {
      location: vscode.ProgressLocation.Window,
      title: "Submitting Job",
    },
    async (progress) => {

      // need to build file first as ExecutionTarget is not reconized at run time
      await promisify(cp.execFile)(`${dotNetSdk.path}`, [
        "build",
        `${projectFilePath}`,
        `-property:ExecutionTarget=${target}`,
      ])
        .then(async (edit) => {
          if (!edit || !workspaceInfo) {
            throw Error;
          }

          let args = ["dotnet", "run", "--no-build"];

          args.push("--project");
          args.push(projectFilePath as string);

          args.push("--");
          args.push("submit");

          args.push("--subscription");
          args.push(workspaceInfo["subscriptionId"]);

          args.push("--resource-group");
          args.push(workspaceInfo["resourceGroup"]);

          args.push("--workspace");
          args.push(workspaceInfo["workspace"]);

          args.push("--target");
          args.push(target);

          args.push("--output");
          args.push("Id");

          args.push("--job-name");
          args.push(jobName||"");

          const token = await credential.getToken(
            "https://quantum.microsoft.com/.default"
          );

          args.push("--aad-token");
          args.push(token.token);

          args.push("--location");
          args.push(workspaceInfo["location"]);

          const packageInfo = getPackageInfo(context);
          const vscode_version = packageInfo?.version;

          args.push("--user-agent");
          args.push(`quantum-devkit-vscode/${vscode_version}`);

          if (programArguments) {
            args = args.concat(programArguments.split(" "));
          }

          await promisify(cp.execFile)(`${dotNetSdk.path}`, args)
            .then((job) => {
              if (!job || !workspaceInfo) {
                throw Error;
              }
              // job.stdout returns job id with an unnecessary space and new line char
              const jobId = job.stdout.slice(0, -2);
              vscode.window.showInformationMessage(`Job Id: ${jobId}`);

              const timeStamp = new Date().toISOString();

              const jobDetails = {
                submissionTime: timeStamp,
                subscriptionId: workspaceInfo["subscriptionId"],
                resourceGroup: workspaceInfo["resourceGroup"],
                workspace: workspaceInfo["workspace"],
                location: workspaceInfo["location"],
                jobId: jobId,
                name: jobName ? jobName : undefined,
                provider: provider,
                target: target,
                programArguments: programArguments
                  ? programArguments
                  : undefined,
              };

              // Add job to the extension's panel
              const locallySubmittedJobs: any = context.workspaceState.get(
                "locallySubmittedJobs"
              );
              const updatedSubmittedJobIds = locallySubmittedJobs
                ? [jobDetails, ...locallySubmittedJobs]
                : [jobDetails];
              context.workspaceState.update(
                "locallySubmittedJobs",
                updatedSubmittedJobIds
              );
              jobsSubmittedProvider.refresh(context);
              vscode.commands.executeCommand("quantum-jobs.focus");

              // update targetsSubmissionDates with new time stamp for target
              // this enables users to see most recent targets first when submitting jobs
              let targetSubmissionDates: any = context.workspaceState.get(
                "targetSubmissionDates"
              );
              if (!targetSubmissionDates){
                targetSubmissionDates={};
              }
              targetSubmissionDates[target] = timeStamp;
              context.workspaceState.update(
                "targetSubmissionDates",
                targetSubmissionDates
              );
            })
            .then(undefined, (err) => {
              console.error("I am a runtime error ");
              console.error(err);
              console.log(err.stdout);
              vscode.window.showErrorMessage(err.stderr);
            });
        })
        .then(undefined, (err) => {
          console.error("I am a compilation error");
          console.error(err);
          console.error(err.stdout);
          vscode.window.showErrorMessage(err.message);
        });
    }
  );
}


async function getJob(context: vscode.ExtensionContext,
    credential: InteractiveBrowserCredential | AzureCliCredential,
    jobId:string,
    workSpaceStatusBarItem: vscode.StatusBarItem
    )
    {
        let jobResponse:any;

        let cachedJobs = context.workspaceState.get("cachedJobs") as any;
        if (cachedJobs && cachedJobs[jobId]) {
            return cachedJobs[jobId];
          }
          await vscode.window.withProgress(
            {
              location: vscode.ProgressLocation.Notification,
              title: "Loading Output...",
              cancellable: false,
            },
            async (progress, token2) => {
        let workspaceInfo = await getWorkspaceInfo(context, credential,workSpaceStatusBarItem);
        if(!workspaceInfo){
            return;
        }

        if (!cachedJobs) {
            cachedJobs = {};
          }


              const quantumJobClient = getQuantumJobClient(workspaceInfo[0], credential);

              try{
              jobResponse = await quantumJobClient.jobs.get(jobId);
                }
              catch(e:any){
                if(e.statusCode === 404){
                    vscode.window.showErrorMessage("Change your workspace to where your job was submitted from.");
                }
                else if(e.statusCode === 403){
                    vscode.window.showErrorMessage("Change your account to where your job was submitted from.");
                }
                else{
                    vscode.window.showErrorMessage(e.message);
                }
                return;
              }

              // if job suceeded store output
              if(jobResponse.status ==="Succeeded"){
                cachedJobs[jobId]= jobResponse;
                context.workspaceState.update("cachedJobs", cachedJobs);
              }
            }
          );
          return jobResponse;

}

// if users requests job results from the Command Palette, jobId param will need to be input by user
// if users requests job results from the panel, jobId will be passed
export async function getJobResults(
  context: vscode.ExtensionContext,
  credential: InteractiveBrowserCredential | AzureCliCredential,
  workSpaceStatusBarItem: vscode.StatusBarItem,
  jobId?: string

) {

  // inputBox for a user calling from Command Palette
  if (!jobId) {
    jobId = await vscode.window.showInputBox({
      prompt: "Enter Job Id",
      ignoreFocusOut: true,
    });
  }
  if (!jobId) {
    return;
  }
  const job = await getJob(context,credential,jobId,workSpaceStatusBarItem);

  if (job === undefined) {
    return;
  }
// cancelling or cancelled jobs will just create a notification
  if(job.status ==="Cancelled"){
    vscode.window.showInformationMessage(`Job status: ${job.status}`);
    return;
  }

  else if (job.isCancelling){
    vscode.window.showInformationMessage(`Job status: Cancellation Requested`);
    return;
  }
// other unsuccessful jobs will result in a notification with the option to cancel
  else if(job.status !=="Succeeded"){
    const userInput = await vscode.window.showInformationMessage(`Job status: ${job.status}`,{}, ...["Cancel Job","Done"]);

    if (userInput === "Cancel Job"){

        const userInputConfirmation = await vscode.window.showInformationMessage(`Are you sure you want to cancel your job?`,{}, ...["Yes","No"]);

        if(userInputConfirmation==="Yes"){
        let workspaceInfo = await getWorkspaceInfo(context, credential,workSpaceStatusBarItem);
        if (!workspaceInfo){
          return;
        }

        const quantumJobClient = getQuantumJobClient(workspaceInfo[0], credential);
        await quantumJobClient.jobs.cancel(jobId);
  }

    }
    return;
  }
  // Succeeded jobs will call the output data uri for the result data
  https
  .get(job.outputDataUri || "", function (response) {
    response.on("data", (chunk: number[]) => {
      const outputString = String.fromCharCode(...chunk) + "\n";
      if (outputString.includes("Histogram")) {
        const rawJsonOutput = JSON.parse(outputString);
        const histArrayLength = rawJsonOutput["Histogram"].length;
        const refinedJson: { [key: string]: any } = { Histogram: {} };

        for (let i = 0; i < histArrayLength; i += 2) {
          const key = rawJsonOutput["Histogram"][i];
          const value = rawJsonOutput["Histogram"][i + 1];
          refinedJson["Histogram"][key] = value;
        }
        openReadOnlyJson({ label: `results-${job.name}`, fullId: jobId as string}, refinedJson);

        }
        }).on("error", function (err) {
          vscode.window.showErrorMessage(err.message);
          });
    });
    }


export async function getJobDetails(
    context: vscode.ExtensionContext,
    credential: InteractiveBrowserCredential | AzureCliCredential,
    jobId: string,
    workSpaceStatusBarItem: vscode.StatusBarItem
  ) {
    const job = await getJob(context,credential,jobId,workSpaceStatusBarItem);

    if (job === undefined) {
      return;
    }

  await openReadOnlyJson(
    { label:job.name, fullId: jobId },
    job
  );
}
