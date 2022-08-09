import * as vscode from "vscode";
import {
  AzureCliCredential,
  InteractiveBrowserCredential,
  AccessToken
} from "@azure/identity";
import { TextEncoder } from "util";
import {workspaceInfo} from "./commands";
// import fetch from 'node-fetch';
import * as https from "https";

const selectionStepEnum = {
  SUBSCRIPTION:1,
  RESOURCE_GROUP:2,
  WORKSPACE:3
};

const totalStepsEnum = {
WORKSPACE_AND_JOB:7,
JUST_WORKSPACE:3
};

export async function getWorkspaceFromUser(
    context: vscode.ExtensionContext,
    credential: InteractiveBrowserCredential | AzureCliCredential,
    workSpaceStatusBarItem: vscode.StatusBarItem,
    totalSteps: number
  ) {
    // get access token
    let token: AccessToken;
    await vscode.window.withProgress(
        {
          location: vscode.ProgressLocation.Notification,
          title: "Loading Azure resources...",
        },
        async (progress) => {
        token = await credential.getToken(
        "https://management.azure.com/.default",
        );
      });

      return new Promise<void>( async (resolve, reject)=>{

    const currentworkspaceInfo: workspaceInfo | unknown = context.workspaceState.get("workspaceInfo");
    const options:any = {
      headers: {
        Authorization: `Bearer ${token.token}`,
      },
      resolveWithFullResponse:true
    };

    let subscriptionId: any;
    let resourceGroup: any;
    let workspace: any;
    let location: any;
    let finished = false;

    const quickPick = vscode.window.createQuickPick();
    // if user is submitting job, total steps will be 7, otherwise 3
    quickPick.totalSteps = totalSteps;

    await setupSubscriptionIdQuickPick(quickPick, currentworkspaceInfo, options);
    quickPick.onDidAccept(async () => {
      const selection = quickPick.selectedItems[0];
      // user selects subscription, now set up resource group selection
      if (quickPick.step === selectionStepEnum.SUBSCRIPTION) {
        if (!selection || !selection["description"]) {
          reject();
          return;
        }
        subscriptionId = selection["description"];
        await setupResourceGroupQuickPick(
          quickPick,
          currentworkspaceInfo,
          subscriptionId,
          options
        );
        // user selects resource group, now set up workspace selection
      } else if (quickPick.step === selectionStepEnum.RESOURCE_GROUP) {
        if (!selection || !selection["label"]) {
          reject();
          return;
        }
        resourceGroup = selection["label"];
        await setupWorkspaceQuickPick(quickPick, subscriptionId, resourceGroup, options);
      }
      // final step
      else if (quickPick.step === selectionStepEnum.WORKSPACE) {
        if (!selection || !selection["label"]) {
              reject();
              return;
            }
            workspace = selection.label;
        location = selection.description;

        // update the locally saved workspace state
        context.workspaceState.update("workspaceInfo", {
          subscriptionId: subscriptionId,
          resourceGroup: resourceGroup,
          workspace: workspace,
          location: location,
        });
        finished = true;
        quickPick.dispose();
        // write the config file cotaining workspace details
        await writeConfigFile(context);
        workSpaceStatusBarItem.text = `Azure Workspace: ${workspace}`;
        workSpaceStatusBarItem.show();
        resolve();
      }
    });

    quickPick.onDidTriggerButton(async (button) => {
      // resource group back button pressed, go back to subscription id
      if (quickPick.step === selectionStepEnum.RESOURCE_GROUP) {
        await setupSubscriptionIdQuickPick(quickPick, currentworkspaceInfo, options);
      }
      // workspaces back button pressed, go back to resource group
      if (quickPick.step === selectionStepEnum.WORKSPACE) {
        await setupResourceGroupQuickPick(
          quickPick,
          currentworkspaceInfo,
          subscriptionId,
          options
        );
      }
    });

    quickPick.onDidHide(()=>{
      if(!finished){
          reject();
      }
    });


  });
  }



  async function setupResourceGroupQuickPick(
    quickPick: vscode.QuickPick<vscode.QuickPickItem>,
    currentworkspaceInfo: workspaceInfo | unknown,
    subscriptionId: string,
    options:any
  ) {
    quickPick.items = [];
    quickPick.step = selectionStepEnum.RESOURCE_GROUP;
    quickPick.title = "Select Resource Group";
    quickPick.value = "";
    quickPick.buttons = [vscode.QuickInputButtons.Back];
    quickPick.enabled = false;
    quickPick.busy = true;
    quickPick.show();


    const rgJSON:any = await new Promise((resolve, reject)=>{
            //@ts-ignore
      const req = https.get(`https://management.azure.com/subscriptions/${subscriptionId}/resourcegroups?api-version=2020-01-01`, options, (res:any) => {
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

    quickPick.value = "";
    if (quickPick.step===selectionStepEnum.RESOURCE_GROUP){
      quickPick.items = rgJSON.value.map((rg: any) => {
          if (
          currentworkspaceInfo &&
          (currentworkspaceInfo as any)["resourceGroup"] === rg.name && quickPick.step ===selectionStepEnum.RESOURCE_GROUP
          ) {
              quickPick.value = rg.name;
          }
          return { label: rg.name };
      });
      quickPick.enabled = true;
      quickPick.busy = false;
      quickPick.show();
  }
  }

  async function setupSubscriptionIdQuickPick(
    quickPick: vscode.QuickPick<vscode.QuickPickItem>,
    currentworkspaceInfo: workspaceInfo | unknown,
    options: any
  ) {
    quickPick.items = [];
    quickPick.step = selectionStepEnum.SUBSCRIPTION;
    quickPick.title = "Select Subscription";
    quickPick.enabled = false;
    quickPick.ignoreFocusOut = true;
    quickPick.busy = true;
    quickPick.buttons = [];
    quickPick.value = "";
    quickPick.show();

    const subscriptionsJSON:any = await new Promise((resolve, reject)=>{
            //@ts-ignore
      const req = https.get("https://management.azure.com/subscriptions?api-version=2020-01-01", options, (res:any) => {
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

    if (quickPick.step===selectionStepEnum.SUBSCRIPTION){
      quickPick.items = subscriptionsJSON.value.map((subscription: any) => {
        if (
          currentworkspaceInfo &&
          (currentworkspaceInfo as any)["subscriptionId"] ===
              subscription.subscriptionId && quickPick.step ===selectionStepEnum.SUBSCRIPTION
          ) {
              quickPick.value = subscription.displayName;
          }
          return {
          label: subscription.displayName,
          description: subscription.subscriptionId,
          };
      });
  }
    quickPick.enabled = true;
    quickPick.busy = false;

    quickPick.show();
  }

  async function setupWorkspaceQuickPick(quickPick: vscode.QuickPick<vscode.QuickPickItem>, subscriptionId:string, resourceGroup:string, options:any){
    quickPick.items = [];
    quickPick.step = selectionStepEnum.WORKSPACE;
    quickPick.title = "Select Workspace";
    quickPick.value = "";
    quickPick.buttons = [vscode.QuickInputButtons.Back];
    quickPick.enabled = false;
    quickPick.busy = true;
    quickPick.show();

    const workspacesJSON:any = await new Promise((resolve, reject)=>{
      //@ts-ignore
      const req = https.get(`https://management.azure.com/subscriptions/${subscriptionId}/resourcegroups/${resourceGroup}/resources?api-version=2020-01-01`, options, (res:any) => {
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

    const quantumWorkspaces = workspacesJSON.value.filter((workspace: any) => {
      if (workspace["type"].includes("Quantum")) {
        return true;
      }
      return false;
    });
    if (quickPick.step===selectionStepEnum.WORKSPACE){
      quickPick.items = quantumWorkspaces.map((workspace: any) => {
        return {
          label: workspace.name,
          description: workspace.location,
        };
      });
    }
      quickPick.enabled = true;
      quickPick.busy = false;
      quickPick.show();

  }


  async function writeConfigFile(context:vscode.ExtensionContext){
    const wsPath = vscode.workspace.workspaceFolders
        ? vscode.workspace.workspaceFolders[0].uri.fsPath
        : undefined;
      // gets the path of the first workspace folder
      const filePath = vscode.Uri.file(wsPath + "/azurequantumconfig.json");
      const jsonWorkspaceInfo = JSON.stringify(
        context.workspaceState.get("workspaceInfo"),
        undefined,
        " ".repeat(4)
      );
      await vscode.workspace.fs.writeFile(
        filePath,
        new TextEncoder().encode(jsonWorkspaceInfo)
      );
}
