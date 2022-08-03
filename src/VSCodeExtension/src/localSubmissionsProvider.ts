import * as vscode from "vscode";

export class LocalSubmissionsProvider implements vscode.TreeDataProvider<LocalSubmissionItem> {
    private _onDidChangeTreeData: vscode.EventEmitter<LocalSubmissionItem | undefined | null | void> = new vscode.EventEmitter<LocalSubmissionItem | undefined | null | void>();
    data: LocalSubmissionItem[];
    readonly onDidChangeTreeData: vscode.Event<LocalSubmissionItem | undefined | null | void> = this._onDidChangeTreeData.event;


    constructor(context: vscode.ExtensionContext) {
         const jobs: any[] = context.workspaceState.get("locallySubmittedJobs")|| [];
         this.data = this.createTree(jobs);
        }

    createTree(jobs:any[]){
        return jobs.map((job:any)=> {
            return new LocalSubmissionItem(job);
            });
        }

    getTreeItem(element: LocalSubmissionItem): vscode.TreeItem | Thenable<vscode.TreeItem> {
        return element;
    }

    getChildren(element?: LocalSubmissionItem | undefined) {
        if (element === undefined) {
            return this.data;
        }
    }
    refresh(context: vscode.ExtensionContext): void {
        const jobs:string[] = context.workspaceState.get("locallySubmittedJobs")|| [];
        this.data = this.createTree(jobs);
        this._onDidChangeTreeData.fire();
        }
}



export class LocalSubmissionItem extends vscode.TreeItem{
    jobDetails: any;
    // need fullId parameter to use openReadOnlyJson from @microsoft/vscode-azext-utils package
    fullId: string;
    constructor(job: any) {
        const time = job["submissionTime"];
        const displayTime = `${time.substring(0,10)}, ${time.substring(11,16)}`;
        const label = `${displayTime} | ${job['jobId']}`;
        super(label);
        this.description = job['name']? `${job['name']}` :``;
        this.tooltip = `${job['name']||""} ${job['target']}`;
        this.contextValue = "LocalSubmissionItem";
        this.jobDetails = job;
        this.fullId = job['jobId'];
}
}
