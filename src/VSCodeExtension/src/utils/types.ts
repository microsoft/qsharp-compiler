export type workspaceInfo = {
    subscriptionId: string;
    resourceGroup: string;
    workspace: string;
    location: string;
  };
  export type configFileInfo ={
    workspaceInfo:workspaceInfo | undefined,
    exitRequest:boolean| undefined
  };
  