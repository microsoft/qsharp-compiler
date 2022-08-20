import { QuantumJobClient } from "@azure/quantum-jobs";
import {workspaceInfo} from "./utils/types";

export function getQuantumJobClient(workspaceInfo: workspaceInfo, credential: any) {
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