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
import {workspaceInfo} from "./utils/types";
import {cancelJob} from "./cancelJob";
import {getQuantumJobClient} from "./quantumJobClient";



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

// submitting program to azure quantum
export async function submitJob(
  context: vscode.ExtensionContext,
  dotNetSdk: DotnetInfo,
  jobsSubmittedProvider: LocalSubmissionsProvider,
  credential: InteractiveBrowserCredential | AzureCliCredential,
  workspaceStatusBarItem: vscode.StatusBarItem,
  workspaceInfo:workspaceInfo|undefined,
  projectFiles: any[],
  totalSteps: number
) {

    if (!workspaceInfo){
      workspaceInfo = await getWorkspaceFromUser(context, credential,workspaceStatusBarItem, totalSteps);
    }
    if (!workspaceInfo){
        return;
    }

  const quantumJobClient = getQuantumJobClient(workspaceInfo, credential);

  const {csproj, jobName, provider, target, programArguments } = await getJobInfoFromUser(context,quantumJobClient, workspaceInfo, totalSteps, projectFiles);

  let projectFilePath:string;
  // Change csproj file foward slashes to backslashes for dotnet operations
  if(csproj){
  projectFilePath = csproj.replace(/[/]/g, "\\").substring(1);
}
else{
  return;
}
let jobId:string|undefined;
  await vscode.window.withProgress(
    {
      location: vscode.ProgressLocation.Notification,
      title: `Submitting to ${target}...`,
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

          args.push("--user-agent");
          args.push("VSCODE");

          if (programArguments) {
            args = args.concat(programArguments.split(" "));
          }

          await promisify(cp.execFile)(`${dotNetSdk.path}`, args)
            .then(async (job) => {
              if (!job || !workspaceInfo) {
                throw Error;
              }
              // job.stdout returns job id with an unnecessary space and new line char
              jobId = job.stdout.trim();
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
            //Send runtime and build errors to Azure Quantum Output Channel
            .then(undefined, async(err) => {
              console.error("I am a runtime error ");
              let outputChannel:vscode.OutputChannel|undefined = context.workspaceState.get("outputChannel");
              if(!outputChannel || !outputChannel["append"]){
                outputChannel = await vscode.window.createOutputChannel("Azure Quantum", "typescript");
                context.workspaceState.update("outputChannel", outputChannel);
              }
              vscode.window.showErrorMessage("Submission failed: Runtime Error");
              outputChannel.appendLine(err.stderr);
              outputChannel.show();
            });
        })
        .then(undefined, async (err) => {
          console.error("I am a compilation error");
          let outputChannel:vscode.OutputChannel|undefined = context.workspaceState.get("outputChannel");
          if(!outputChannel || !outputChannel["append"]){
            outputChannel = await vscode.window.createOutputChannel("Azure Quantum","typescript");
            context.workspaceState.update("outputChannel", outputChannel);
          }
          vscode.window.showErrorMessage("Submission failed: Build Error");
          outputChannel.appendLine(err.stdout);
          outputChannel.show();
        });
    }
  );
  if(jobId){
    const message = jobName? `Job Submitted: ${jobName} (${jobId})`:`Job submitted: ${jobId}`;
    const userInput = await vscode.window.showInformationMessage(message, {}, ...["Cancel Job"]);
    if (userInput === "Cancel Job"){
      await cancelJob(workspaceInfo, credential, jobId);
    }
  }
}


async function getJob(
    credential: InteractiveBrowserCredential | AzureCliCredential,
    workspaceInfo:workspaceInfo,
    jobId:string,
    )
    {
        let jobResponse:any;

          await vscode.window.withProgress(
            {
              location: vscode.ProgressLocation.Notification,
              title: "Loading Output...",
              cancellable: false,
            },
            async (progress, token2) => {


              const quantumJobClient = getQuantumJobClient(workspaceInfo, credential);

              try{
              jobResponse = await quantumJobClient.jobs.get(jobId);
                }
              catch(e:any){
                if(e.statusCode === 404){
                    vscode.window.showErrorMessage(`${e.message} Check you are in the same workspace you submitted your job from.`);
                }
                else{
                    vscode.window.showErrorMessage(e.message);
                }
                return;
              }
            }
          );
          return jobResponse;

}

// if users requests job results from the Command Palette, jobId param will need to be input by user
// if users requests job results from the panel, jobId will be passed
export async function getJobResults(
  credential: InteractiveBrowserCredential | AzureCliCredential,
  workspaceInfo: workspaceInfo,
  totalSteps?:number|undefined,
  jobId?: string

) {
  // inputBox for a user calling Get Job from Command Palette
  if (!jobId) {
    const inputTitle = totalSteps?"Enter Job Id (4/4)":"Enter Job Id";
    vscode.commands.executeCommand("quantum-jobs.focus");
    jobId = await vscode.window.showInputBox({
      ignoreFocusOut: true,
      title: inputTitle
    });
  }
  if (!jobId) {
    return;
  }
  const job = await getJob(credential,workspaceInfo,jobId);

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
// other jobs which have not succeeded will result in a notification with the option to cancel
  else if(job.status !=="Succeeded"){
    const userInput = await vscode.window.showInformationMessage(`Job status: ${job.status}`,{}, ...["Cancel Job"]);

    if (userInput === "Cancel Job"){
      await cancelJob(workspaceInfo, credential, jobId);
    }
    return;
  }
  // Succeeded jobs will call the output data uri for the result data
  https
  .get(job.outputDataUri || "", function (response) {
    response.on("data", (chunk: number[]) => {
      const outputString = String.fromCharCode(...chunk) + "\n";
      // successful jobs will have a histogram field
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
        // If there is an error, need to parse the response to show the
        // message to the user.
        else if(outputString.includes("Error"))
        {
          try{
          const message = outputString.split("<Message>")[1].split("</Message>")[0];
          vscode.window.showErrorMessage(message);
          }
          catch{
            vscode.window.showErrorMessage("Error retrieving job.");
          }
        }
        }).on("error", function (err) {
          vscode.window.showErrorMessage(err.message);
          });
    });
    }


export async function getJobDetails(
    credential: InteractiveBrowserCredential | AzureCliCredential,
    workspaceInfo: workspaceInfo,
    jobId: string
  ) {
    const job = await getJob(credential,workspaceInfo, jobId);

    if (job === undefined) {
      return;
    }

  await openReadOnlyJson(
    { label:job.name, fullId: jobId },
    job
  );
}
