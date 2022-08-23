export const jobParameters = {
    csprojUrl: "/Users/owner/Desktop/sampleQuantumProj/proj1/ParallelQrng.csproj",
	provider: "ionq",
    target: "ionq.simulator",
    name: "tester",
    additionalArgs: "--n-qubits=2"
};

export const jobId_1 = "0d3a5a54-a09e-4428-a83e-f019cefc932a";

export const expectedResult_1 = {
    "Histogram": {
        "[0]": 0.5,
        "[1]": 0.5
    }
};

export const mockLocalPanelJob = {
    collapsibleState:0, 
    contextValue:'LocalSubmissionItem',
    description:'tester',
    fullId:'0d3a5a54-a09e-4428-a83e-f019cefc932a',
    jobDetails:{
        jobId:'0d3a5a54-a09e-4428-a83e-f019cefc932a',
        location:'eastus',
        name:'tester',
        programArguments:undefined,
        provider:'ionq',
        resourceGroup:'AzureQuantum',
        submissionTime:'2022-08-20T20:45:43.055Z',
        subscriptionId:'621181e5-3d0e-42c6-8287-d78d3c7f2629',
        target:'ionq.simulator',
        workspace:'monastest1'
},
label:'2022-08-20, 20:45 | 0d3a5a54-a09e-4428-a83e-f019cefc932a',
tooltip:'Submitted from monastest1 to ionq.simulator',
};

export const expectedDetails_1 = {
    "id": "0d3a5a54-a09e-4428-a83e-f019cefc932a",
    "name": "tester",
    "containerUri": "https://aqbcc64657985e447cbc096b.blob.core.windows.net/quantum-job-0d3a5a54-a09e-4428-a83e-f019cefc932a",
    "inputDataFormat": "microsoft.ionq-ir.v3",
    "inputParams": {
        "shots": "500"
    },
    "providerId": "ionq",
    "target": "ionq.simulator",
    "metadata": {
        "entryPointInput": "{\"Qubits\":null}",
        "outputMappingBlobUri": "https://aqbcc64657985e447cbc096b.blob.core.windows.net/quantum-job-0d3a5a54-a09e-4428-a83e-f019cefc932a/mappingData?sv=2019-02-02&sr=b&sig=s2udtP9a%2FcMvRWL%2ByRqjHepOK5pKxH7EEJEWG%2BiIABc%3D&se=2022-08-24T20%3A45%3A43Z&sp=rcw"
    },
    "outputDataFormat": "microsoft.quantum-results.v1",
    "status": "Succeeded",
    "creationTime": "2022-08-20T20:45:42.943Z",
    "beginExecutionTime": "2022-08-20T20:45:51.602Z",
    "endExecutionTime": "2022-08-20T20:45:51.632Z",
    "cancellationTime": null,
    "errorData": null,
    "costEstimate": {
        "currencyCode": "USD",
        "events": [
            {
                "dimensionId": "gs1q",
                "dimensionName": "1Q Gate Shot",
                "measureUnit": "1q gate shot",
                "amountBilled": 0,
                "amountConsumed": 0,
                "unitPrice": 0
            },
            {
                "dimensionId": "gs2q",
                "dimensionName": "2Q Gate Shot",
                "measureUnit": "2q gate shot",
                "amountBilled": 0,
                "amountConsumed": 0,
                "unitPrice": 0
            }
        ],
        "estimatedTotal": 0
    },
    "isCancelling": false,
    "tags": []
};