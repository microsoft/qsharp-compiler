import * as vscode from "vscode";
import * as excludedTargets from "./utils/excludedTargets.json";
import {workspaceInfo} from "./commands";
import { QuantumJobClient } from "@azure/quantum-jobs";
type target = {
    id: string;
    currentAvailability: string;
    averageQueueTime: number;
    statusPage: string;
  };

type jobMetaData = {provider:string, target:string, jobName:string, programArguments:string};

let updateWorkspace = false;
let quickPickIsHidden = false;

export async function getJobInfoFromUser(context:vscode.ExtensionContext, quantumJobClient:QuantumJobClient, workspaceInfo:workspaceInfo, update=false){

    updateWorkspace = update;
    let provider:string;
    let target:string;
    let jobName:string;
    let programArguments:string;


    let providersAndTargets:any;

    return new Promise<jobMetaData>( async (resolve, reject)=>{
        const quickPick = vscode.window.createQuickPick();
        quickPickIsHidden = false;
        const inputBox = vscode.window.createInputBox();
        inputBox.buttons = [vscode.QuickInputButtons.Back];
        // if user has to select workspace, total steps will be 7, otherwise 4
        inputBox.totalSteps = updateWorkspace?7:4;
        quickPick.totalSteps = updateWorkspace?7:4;
        inputBox.ignoreFocusOut = true;
        quickPick.ignoreFocusOut = true;


        providersAndTargets = await setupProviderQuickPick(quickPick, context, quantumJobClient, workspaceInfo, inputBox);
        if(providersAndTargets===undefined){
            inputBox.dispose();
            quickPick.dispose();
            reject();
        }


         inputBox.onDidAccept(async () => {
          // final step finished, therefore submit job
            if((!updateWorkspace && inputBox.step===4)||(updateWorkspace && inputBox.step===7)){
                programArguments = inputBox.value;
                inputBox.dispose();
                quickPick.dispose();
                resolve({jobName:jobName,provider:provider, target:target,programArguments:programArguments});
            }
          // user input job name so prompt program arguments input
            if ((!updateWorkspace && inputBox.step===3)||(updateWorkspace && inputBox.step===6)){
            jobName = inputBox.value;
            setupProgramArguments(inputBox);
        }
        });

         quickPick.onDidAccept(async()=>{
            const selection = quickPick.selectedItems[0];
            if (!selection || !selection["label"]) {
                inputBox.dispose();
                quickPick.dispose();
                reject();
              }
              // user input target so prompt job name input
              if ((!updateWorkspace && quickPick.step===2)||(updateWorkspace && quickPick.step===5)){
                target = selection["label"];
                quickPick.hide();
                setupNameInputBox(inputBox);
            }
              // user input provider so prompt target input
            if ((!updateWorkspace && quickPick.step===1)||(updateWorkspace && quickPick.step===4)){
                provider = selection["label"];
                setupTargetsQuickPick(quickPick,providersAndTargets,provider);
            }

         });
         inputBox.onDidTriggerButton(async (button) => {
            // user pressed name back button so prompt target input
            if ((!updateWorkspace && inputBox.step===3)||(updateWorkspace && inputBox.step===6)) {
                inputBox.hide();
                setupTargetsQuickPick(quickPick,providersAndTargets,provider);
            }
            // user pressed program arguments back button so prompt job name input
            if ((!updateWorkspace && inputBox.step===4)||(updateWorkspace && inputBox.step===7)) {
                setupNameInputBox(inputBox);
            }
          });


        quickPick.onDidTriggerButton(async (button) => {
            // user pressed target back button so prompt provider input
            if ((!updateWorkspace && quickPick.step===2)||(updateWorkspace && quickPick.step===5)) {
                providersAndTargets = await setupProviderQuickPick(quickPick, context, quantumJobClient, workspaceInfo, inputBox);
                if(providersAndTargets===undefined){
                    inputBox.dispose();
                    quickPick.dispose();
                    reject();
                }
            }
          });
        quickPick.onDidHide(()=>{
            quickPickIsHidden = true;

        });



     });


}

function setupNameInputBox(inputBox:vscode.InputBox){

    inputBox.step = updateWorkspace?6:3;
    inputBox.value = "";
    inputBox.placeholder = "";
    inputBox.title = "Enter a Job Name";
    inputBox.show();

}


function setupProgramArguments(inputBox:vscode.InputBox){

    inputBox.step = updateWorkspace?7:4;
    inputBox.value = "";
    inputBox.placeholder = 'Enter any parameters in the format "--param1=value1 --param2=value2"';
    inputBox.title = "Program Arguments";
    inputBox.show();

}

async function setupProviderQuickPick(quickPick:vscode.QuickPick<vscode.QuickPickItem>,context:vscode.ExtensionContext, quantumJobClient:QuantumJobClient, workspaceInfo:workspaceInfo, inputBox:vscode.InputBox){

    quickPick.items = [];
    quickPick.buttons = [];
    quickPick.step = updateWorkspace?4:1;
    quickPick.title = "Select a Provider";
    quickPick.value = "";
    quickPick.enabled = false;
    quickPick.busy = true;
    quickPick.show();
    quickPickIsHidden = false;


    const providersAndTargets = await getProvidersAndTargets(context, quantumJobClient, workspaceInfo);
    if(providersAndTargets===undefined){
        return;
    }

    const providerList = Object.keys(providersAndTargets).sort((a, b) => {
        return providersAndTargets[b][0]["lastSubmissionDate"] >
          providersAndTargets[a][0]["lastSubmissionDate"]
          ? 1
          : -1;
      });

    quickPick.items= providerList.map((provider:string)=>{
        return {"label":provider};
    });
    quickPick.enabled = true;
    quickPick.busy = false;
    if(quickPickIsHidden){
        quickPick.dispose();
        inputBox.dispose();
    }
    return providersAndTargets;
}


function setupTargetsQuickPick(quickPick:vscode.QuickPick<vscode.QuickPickItem>, providersAndTargets:any, provider:string){
    quickPick.items = (providersAndTargets as { [key: string]: any })[provider].map(
        (t: target) => {
          return {
            label: t.id,
            description: t.currentAvailability,
            detail: `Average Queue Time: ${t.averageQueueTime}`,
          };
        }
      );
    quickPick.step = updateWorkspace?5:2;
    quickPick.title = "Select a Target";
    quickPick.value = "";
    quickPick.buttons = [vscode.QuickInputButtons.Back];
    quickPick.show();
    quickPickIsHidden = false;



}

async function getProvidersAndTargets(context:vscode.ExtensionContext, quantumJobClient:QuantumJobClient, workspaceInfo:workspaceInfo){
  // retrieve targets available in workspace
  const availableTargets: { [key: string]: any } = {};
  const providerStatuses = await quantumJobClient.providers.listStatus();

      let iter;
      try{
      while ((iter = await providerStatuses.next())) {
        if (iter && !iter.value) {
          break;
        }
        // For now, only concern is .qs programs, not json optimization programs
        // so filter out optimization targets
        const targets = iter["value"]["targets"].filter((target: any) => {
          if (excludedTargets["optimizationTargets"].includes(target["id"])) {
            return false;
          }
          return true;
        });
        const provider = iter["value"]["id"];
        if (targets.length > 0) {
          availableTargets[provider] = targets;
        }
      }
}
catch(err:any){
    if(err.statusCode === 403){
        vscode.window.showErrorMessage("Your account is not authorized to use this workspace.");
    }
    else{
    vscode.window.showErrorMessage(err.message||"Error");
}
    return;
}
  let targetSubmissionDates: any = context.workspaceState.get(
                "targetSubmissionDates"
              );
              if (!targetSubmissionDates){
                targetSubmissionDates={};
              }
  for (let provider of Object.keys(availableTargets)) {
    availableTargets[provider].map((t: any) => {
      t["lastSubmissionDate"] = targetSubmissionDates[t["id"]]
        ? new Date(targetSubmissionDates[t["id"]])
        : new Date(2000);
    });
    availableTargets[provider].sort((a: any, b: any) => {
      return b["lastSubmissionDate"] > a["lastSubmissionDate"] ? 1 : -1;
    });
  }

  for (let provider of Object.keys(availableTargets)) {
    availableTargets[provider].sort((a: any, b: any) => {
      return a["currentAvailability"].localeCompare(b["currentAvailability"]);
    });
  }

  return availableTargets;

}
