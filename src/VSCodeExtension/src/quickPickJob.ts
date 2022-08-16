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

type jobMetaData = {provider:string, target:string, jobName:string, programArguments:string, csproj:string};

let submissionStepEnum: {
  CSPROJ: number,
  PROVIDER:number,
  TARGET:number,
  NAME:number,
  PROGRAM_ARGUMENT: number
};

let provider:string;
let _target:string;
let jobName:string;
let csproj:string;
let programArguments:string;
let providersAndTargets:any;

let quickPickIsHidden = false;
let csprojQuickPickFlag=false;

export async function getJobInfoFromUser(context:vscode.ExtensionContext, quantumJobClient:QuantumJobClient, workspaceInfo:workspaceInfo, totalSteps:number, projectFiles:any[]){

  provider="";
  _target="";
  jobName="";
  csproj="";
  programArguments="";
  providersAndTargets="";

    submissionStepEnum = {
      CSPROJ: totalSteps - 4,
      PROVIDER:totalSteps - 3,
      TARGET:totalSteps - 2,
      NAME:totalSteps - 1,
      PROGRAM_ARGUMENT: totalSteps
    };

    if (projectFiles.length>1){
      csprojQuickPickFlag = true;
    }

    return new Promise<jobMetaData>( async (resolve, reject)=>{
        const quickPick = vscode.window.createQuickPick();
        quickPickIsHidden = false;
        const inputBox = vscode.window.createInputBox();
        inputBox.buttons = [vscode.QuickInputButtons.Back];
        inputBox.totalSteps = totalSteps;
        quickPick.totalSteps = totalSteps;
        inputBox.ignoreFocusOut = true;
        quickPick.ignoreFocusOut = true;

        if (csprojQuickPickFlag){
          setupCsProjQuickPick(quickPick, projectFiles);
        }
        else{
          csproj = projectFiles[0]["path"];
          providersAndTargets = await setupProviderQuickPick(quickPick, context, quantumJobClient, workspaceInfo, inputBox);

          if(providersAndTargets===undefined){
            inputBox.dispose();
            quickPick.dispose();
            return reject();
        }
      }


         inputBox.onDidAccept(async () => {
          // final step finished, therefore submit job
            if(inputBox.step ===submissionStepEnum.PROGRAM_ARGUMENT){
                programArguments = inputBox.value;
                inputBox.dispose();
                quickPick.dispose();
                resolve({csproj:csproj,jobName:jobName,provider:provider, target:_target,programArguments:programArguments});
            }
          // user input job name so prompt program arguments input
          if (inputBox.step === submissionStepEnum.NAME){
            jobName = inputBox.value;
            setupProgramArguments(inputBox);
        }
        });

         quickPick.onDidAccept(async()=>{
            const selection = quickPick.selectedItems[0];
            if (!selection || !selection["label"]) {
                inputBox.dispose();
                quickPick.dispose();
                return reject();
              }
              // user input target so prompt job name input
              if (quickPick.step === submissionStepEnum.TARGET){
                _target = selection["label"];
                quickPick.hide();
                setupNameInputBox(inputBox);
            }
              // user input provider so prompt target input
              if (quickPick.step === submissionStepEnum.PROVIDER){
                if(provider !== selection["label"]){
                  _target="";
                }
                provider = selection["label"];
                setupTargetsQuickPick(quickPick,providersAndTargets,provider);
            }
              // user input csProj so prompt provider input
              if (selection["detail"] && quickPick.step === submissionStepEnum.CSPROJ){
                csproj = selection["detail"];
                providersAndTargets = await setupProviderQuickPick(quickPick, context, quantumJobClient, workspaceInfo, inputBox);
            }

         });
         inputBox.onDidTriggerButton(async (button) => {
            // user pressed name back button so prompt target input
            if (inputBox.step === submissionStepEnum.NAME) {
              inputBox.hide();
              setupTargetsQuickPick(quickPick,providersAndTargets,provider);
            }
            // user pressed program arguments back button so prompt job name input
            if (inputBox.step === submissionStepEnum.PROGRAM_ARGUMENT) {
              setupNameInputBox(inputBox);
            }
          });


        quickPick.onDidTriggerButton(async (button) => {
          if (csprojQuickPickFlag && quickPick.step === submissionStepEnum.PROVIDER) {
            setupCsProjQuickPick(quickPick, projectFiles);
          }
          // user pressed target back button so prompt provider input
            if (quickPick.step === submissionStepEnum.TARGET) {
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

function setupCsProjQuickPick(quickPick:vscode.QuickPick<vscode.QuickPickItem>, projectFiles:any[]){

  quickPick.buttons = [];
  provider="";
  quickPick.step = submissionStepEnum.CSPROJ;
  quickPick.matchOnDetail = true;
  quickPick.value = csproj?csproj:"";
  quickPick.placeholder = "";
  quickPick.title = "Select a .csproj file";
  quickPick.items = projectFiles.map((file)=>{
    const csProjName = file["path"].split("/").pop();
    return {
      label: csProjName,
      detail: file["path"],
    };
  });
  quickPick.show();



}


function setupNameInputBox(inputBox:vscode.InputBox){

    inputBox.step = submissionStepEnum.NAME;
    inputBox.value = jobName?jobName:"";
    inputBox.placeholder = "";
    inputBox.title = "(Optional) Enter a Job Name";
    inputBox.show();

}


function setupProgramArguments(inputBox:vscode.InputBox){

    inputBox.step = submissionStepEnum.PROGRAM_ARGUMENT;
    inputBox.value = "";
    inputBox.placeholder = 'Enter any parameters in the format "--param1=value1 --param2=value2"';
    inputBox.title = "(Optional) Enter Program Arguments";
    inputBox.show();

}

async function setupProviderQuickPick(quickPick:vscode.QuickPick<vscode.QuickPickItem>,context:vscode.ExtensionContext, quantumJobClient:QuantumJobClient, workspaceInfo:workspaceInfo, inputBox:vscode.InputBox){

    quickPick.items = [];
    quickPick.matchOnDetail = false;
    quickPick.buttons = csprojQuickPickFlag?[vscode.QuickInputButtons.Back]:[];
    quickPick.step = submissionStepEnum.PROVIDER;
    quickPick.title = "Select a Provider";
    quickPick.value = provider?provider:"";
    quickPick.enabled = false;
    quickPick.busy = true;
    quickPick.show();
    quickPickIsHidden = false;
    _target="";

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
    quickPick.step = submissionStepEnum.TARGET;
    quickPick.title = "Select a Target";
    quickPick.value = _target?_target:"";
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
