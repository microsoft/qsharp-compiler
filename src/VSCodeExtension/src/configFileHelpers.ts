import * as vscode from 'vscode';
import {workspaceStatusEnum} from "./utils/constants";
import {getWorkspaceFromUser} from "./quickPickWorkspace";
import {workspaceInfo, configFileInfo} from "./utils/types";
import {setupAuthorizedWorkspaceStatusButton} from "./workspaceStatusButtonHelpers";
import * as https from "https";


// If config not present, queries user for workspace information
// If config present, verifies config. If verification fails, 
// does NOT automatically query user for workspace information
export async function setWorkspace(context:vscode.ExtensionContext, credential:any,workspaceStatusBarItem:vscode.StatusBarItem, extraSteps:number){
    let {workspaceInfo, exitRequest} = await getConfig(context, credential, workspaceStatusBarItem);
    if(exitRequest){
      return;
    }
    if(!workspaceInfo){
      // task steps + three workspace steps
      const totalSteps =  extraSteps+3;
      workspaceInfo = await getWorkspaceFromUser(context, credential, workspaceStatusBarItem, totalSteps);
    }
    return workspaceInfo;
  }


// Prompts user to change their workspace if they are in an unauthoritzed
// workspace
export async function handleUnauthorizedConfig(context:vscode.ExtensionContext, credential:any, workspaceStatusBarItem:vscode.StatusBarItem){
    const userInput = await vscode.window.showErrorMessage("You do not have access to this workspace, or it doesn't exist.", {}, ...["Change Workspace"]);
    if (userInput === "Change Workspace"){
      const workspaceInfo = await getWorkspaceFromUser(context, credential, workspaceStatusBarItem, 3, true);
      if(workspaceInfo){
        return true;
      }
      return false;
    }
    return false;
  }


  // Verify the provided workspace details exist in a user's account
// If they do not, prompt user to change workspace
export async function verifyConfig(context: vscode.ExtensionContext, credential:any, workspaceInfo:workspaceInfo, workspaceStatusBarItem: vscode.StatusBarItem){
    const {subscriptionId, resourceGroup, workspace, location} = workspaceInfo;
    let workspaceExistsFlag;
  
    await vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      title: "Setting up workspace...",
      "cancellable":true
      }, async (progress, cancel) => {
    const token = await credential.getToken(
      "https://management.azure.com/.default",
      );
    const options:any = {
      headers: {
        Authorization: `Bearer ${token.token}`,
  
      },
      resolveWithFullResponse:true
    };
  
      try{
        const workspaceResponse:any = await new Promise((resolve, reject)=>{
              //@ts-ignore
        const req = https.get(`https://management.azure.com/subscriptions/${subscriptionId}/resourcegroups/${resourceGroup}/resources?api-version=2020-01-01&$filter=resourceType eq 'Microsoft.Quantum/Workspaces' and name eq '${workspace}'`, options, (res:any) => {
          let responseBody = '';
  
          res.on('data', (chunk:any) => {
              responseBody += chunk;
          });
  
          res.on('end', () => {
              resolve(JSON.parse(responseBody));
          });
      });
  
      req.on('error', (err:any) => {
          reject(err);
      });
      });
      // The workspaceResponse.value is an array, which contains resources
      // matching the query. Here, this will be workspaces of the 
      // resourceType 'Microsoft.Quantum/Workspaces' and of the provided
      // workspace name
       workspaceExistsFlag = workspaceResponse?.value?.length > 0;
       // Location is not specified in above query, but is needed for
       // Quantum Job Client, so validate location manually
       if (workspaceResponse?.value[0].location !== location){
        workspaceExistsFlag = false;
       }
    }
    catch(err:any){
      workspaceExistsFlag = false;
    }
  });
      if (!workspaceExistsFlag){
        context.workspaceState.update("workspaceStatus", workspaceStatusEnum.UNAUTHORIZED);
        workspaceStatusBarItem.text = `$(error) Azure Workspace: ${workspace}`;
        workspaceStatusBarItem.command = "quantum.changeWorkspace";
        workspaceStatusBarItem.tooltip = "Unauthorized Workspace";
        workspaceStatusBarItem.show(); 
        await handleUnauthorizedConfig(context, credential, workspaceStatusBarItem);
        return false;
  }
  setupAuthorizedWorkspaceStatusButton(context,workspaceInfo, workspaceStatusBarItem);
  return true; 
  
  }


// returns an object with the workspaceInfo, if succ
export async function getConfig(context:vscode.ExtensionContext, credential:any, workspaceStatusBarItem:vscode.StatusBarItem, validateFlag=true):Promise<configFileInfo> {

    const configFileInfo:configFileInfo = {
      workspaceInfo:undefined,
      exitRequest:false
    };
  
    const workspaceStatus = context.workspaceState.get("workspaceStatus");
  
    // If a user is unauthorized and validation is set, prompt user to
    // change workspaces. Return with an exitRequest to stop the flow.
    if(workspaceStatus === workspaceStatusEnum.UNAUTHORIZED && validateFlag){
      await handleUnauthorizedConfig(context, credential, workspaceStatusBarItem);
      configFileInfo["exitRequest"]=true;
      return configFileInfo;
    }
  
    let workspaceInfo:workspaceInfo;
    const configFile = await vscode.workspace.findFiles(
      "**/azurequantumconfig.json"
    );
  
    // no config file present, but this is not function stopping as 
    // the user will be queried for a workspace
    if(configFile.length ===0){
      return configFileInfo;
    }
  
    // If multiple config files are present, stop the function as more 
    // than one config in a user's workspace is not permitted at this time.
    if(configFile.length>1){
      configFileInfo.exitRequest = true;
      vscode.window.showWarningMessage("Only one azurequantumconfig.json file is allowed in a workspace.");
      return configFileInfo;
    }
      const workspaceInfoChunk: any = await vscode.workspace.fs.readFile(
        configFile[0]
      );
      
      // try to pull subscription, resource groupm, workspace, and location
      // from config file.
      try{
      workspaceInfo = JSON.parse(String.fromCharCode(...workspaceInfoChunk));
      // Verify azurequantumconfig.json file has necessary fields
      if (!workspaceInfo["subscriptionId"]||!workspaceInfo["resourceGroup"]||!workspaceInfo["workspace"]||!workspaceInfo["location"]){
          throw Error;
      }
  }
  // The config file is corrupted or doesn't have the correct fields. 
  // This is not function stopping as the user will be queried for a workspace
      catch{
          if(validateFlag){
          vscode.window.showWarningMessage("Invalid azurequantumconfig.json format");
          }
          return configFileInfo;
      }
  // If config file hasn't been verified and validation is set, query the
  // users account for the given workspace. If the user is not authorized
  // stop the function flow.
      if(workspaceStatus === workspaceStatusEnum.UNKNOWN && validateFlag){
        const verificationResult = await verifyConfig(context, credential, workspaceInfo, workspaceStatusBarItem);
        if(!verificationResult){
          configFileInfo["exitRequest"]=true;
          return configFileInfo;
      }
      }
      configFileInfo["workspaceInfo"]=workspaceInfo;
      return configFileInfo;
  }
