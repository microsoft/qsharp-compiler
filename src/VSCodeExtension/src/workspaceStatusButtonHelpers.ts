import * as vscode from 'vscode';
import {workspaceStatusEnum} from "./utils/constants";
import {workspaceInfo} from "./utils/types";
import {getConfig} from "./configFileHelpers";

export async function setupUnknownWorkspaceStatusButton(context:vscode.ExtensionContext, credential:any, workspaceStatusBarItem:vscode.StatusBarItem) {
    context.workspaceState.update("workspaceStatus", workspaceStatusEnum.UNKNOWN);
    const {workspaceInfo} = await getConfig(context, credential, workspaceStatusBarItem, false);
    if (workspaceInfo?.workspace){
        workspaceStatusBarItem.text = `Azure Workspace: ${workspaceInfo["workspace"]}*`;
        workspaceStatusBarItem.command = "quantum.getWorkspace";
        workspaceStatusBarItem.tooltip = "Workspace has not been verified";
        workspaceStatusBarItem.show(); 
    }
    else{
        workspaceStatusBarItem.text = "Connect to Azure Workspace";
        workspaceStatusBarItem.command = "quantum.getWorkspace";
        workspaceStatusBarItem.tooltip = "";
        workspaceStatusBarItem.show();
    }
}

export function setupDefaultWorkspaceStatusButton(context:vscode.ExtensionContext, workspaceStatusBarItem:vscode.StatusBarItem) {
    context.workspaceState.update("workspaceStatus", workspaceStatusEnum.UNKNOWN);
    workspaceStatusBarItem.text = "Connect to Azure Workspace";
    workspaceStatusBarItem.command = "quantum.getWorkspace";
    workspaceStatusBarItem.tooltip = "";
    workspaceStatusBarItem.show();
}

export function setupAuthorizedWorkspaceStatusButton(context:vscode.ExtensionContext,workspaceInfo:workspaceInfo, workspaceStatusBarItem:vscode.StatusBarItem){
    context.workspaceState.update("workspaceStatus", workspaceStatusEnum.AUTHORIZED);
    workspaceStatusBarItem.text = `$(pass) Azure Workspace: ${workspaceInfo.workspace}`;
    workspaceStatusBarItem.command = "quantum.changeWorkspace";
    workspaceStatusBarItem.tooltip = "Workspace is verified";
    workspaceStatusBarItem.show(); 
}