import * as vscode from 'vscode';
import {workspaceInfo} from "./utils/types";
import {getQuantumJobClient} from "./quantumJobClient";

export async function cancelJob(workspaceInfo:workspaceInfo, credential:any, jobId:string){
    const userInputConfirmation = await vscode.window.showInformationMessage(`Are you sure you want to cancel your job?`,{}, ...["Yes","No"]);
    if(userInputConfirmation==="Yes"){
        const quantumJobClient = getQuantumJobClient(workspaceInfo, credential);
        await quantumJobClient.jobs.cancel(jobId);
    }
}
