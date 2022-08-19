// MSAL treats the Microsoft account system (Live, MSA) as another tenant
// within the Microsoft identity platform. The tenant id of the Microsoft
// account tenant is 9188040d-6c67-4c5b-b112-36a304b66dad
export const MSA_ACCOUNT_TENANT = "9188040d-6c67-4c5b-b112-36a304b66dad";

export const configIssueEnum = {
    NO_CONFIG:1,
    INVALID_CONFIG:2,
    MULTIPLE_CONFIGS:3
  };
  
export const workspaceStatusEnum = {
    UNKNOWN:1,
    AUTHORIZED:2,
    UNAUTHORIZED:3
}