import * as assert from 'assert';
import * as sinon from 'sinon';
import { expect } from 'chai';
import { window, extensions, commands} from 'vscode';
import * as vscode from "vscode";
import { workspace_1, workspace_2 } from '../../utils/test-utils/workspaces';
import { expectedResult_1, expectedDetails_1, mockLocalPanelJob, jobId_1, jobParameters} from '../../utils/test-utils/jobData';
import { EventEmitter } from '../../utils/test-utils/events';
import {describe, it} from "mocha";
import {findWorkspace, eventuallyOk} from "./testHelpers";


describe('Extension Test Suite', async() => {
	window.showInformationMessage('Start all tests.');
    const started = extensions.getExtension("quantum.quantum-devkit-vscode");
    await started?.activate();
	let createQuickPick: sinon.SinonSpy;
	let acceptQuickPick: EventEmitter<void>;

	it("Should start quantum extension", async () => {
		// activate the extension
		assert.notEqual(started, undefined);
		assert.equal(started?.isActive, true);
	});	

	it("Should register all commands", async () => {
		// get commands
		const commandsList = await commands.getCommands();
		const quantumCommandsList = commandsList.filter(x => x.startsWith("quantum"));
		//assert
		assert.equal(quantumCommandsList.includes("quantum.newProject"), true );
		assert.equal(quantumCommandsList.includes("quantum.installTemplates"), true );
		assert.equal(quantumCommandsList.includes("quantum.openDocumentation"), true );
		assert.equal(quantumCommandsList.includes("quantum.installIQSharp"), true );
		assert.equal(quantumCommandsList.includes("quantum.installIQSharp"), true );
        assert.equal(quantumCommandsList.includes("quantum.connectToAzureAccount"), true );
		assert.equal(quantumCommandsList.includes("quantum.submitJob"), true );
		assert.equal(quantumCommandsList.includes("quantum.jobResultsPalette"), true );
		assert.equal(quantumCommandsList.includes("quantum.changeAzureAccount"), true );
        assert.equal(quantumCommandsList.includes("quantum.changeWorkspace"), true );
	});

	it("Should connect to Azure Account", async () => {
		// tester needs to be logged into Az Cli
		const showInformationMessageStub = sinon.stub(vscode.window, "showInformationMessage");
		await commands.executeCommand("quantum.connectToAzureAccount");
		await new Promise(resolve => setTimeout(resolve, 3000));
		assert.ok(showInformationMessageStub.calledOnce);
		assert(showInformationMessageStub.args[0], "Successfully connected to account.");
	});


	it("Set workspace", async () => {
		prepareStubsQuickPick();
		await commands.executeCommand("quantum.getWorkspace");
		await new Promise(resolve => setTimeout(resolve, 3000));

		const typePicker = await eventuallyOk(() => { 
			expect(createQuickPick.callCount).to.equal(1);
			const picker: vscode.QuickPick<vscode.QuickPickItem> =
			createQuickPick.getCall(0).returnValue;
			return picker;
		  });
		  typePicker.selectedItems = await typePicker.items.filter(i => i.label === workspace_1.subscriptionName);
		  acceptQuickPick.fire();

		  await new Promise(resolve => setTimeout(resolve, 2000));

		  typePicker.selectedItems = await typePicker.items.filter(i => i.label === workspace_1.resourceGroup);
		  acceptQuickPick.fire();
		  await new Promise(resolve => setTimeout(resolve, 2000));

		  typePicker.selectedItems = await typePicker.items.filter(i => i.label === workspace_1.workspace);
		  acceptQuickPick.fire();
		  await new Promise(resolve => setTimeout(resolve, 2000));
		
		// pull workspace from azurequantumconfig.json and test against
		// expected workspace details
		const workspaceInfo = await findWorkspace();
		assert(workspaceInfo["subscriptionId"],workspace_1["subscriptionId"]);
		assert(workspaceInfo["resourceGroup"],workspace_1["resourceGroup"]);
		assert(workspaceInfo["workspace"],workspace_1["workspace"]);
		assert(workspaceInfo["location"],workspace_1["location"]);
		clearStubsQuickPick();
	});


	it("Change workspace", async () => {
		prepareStubsQuickPick();
		await commands.executeCommand("quantum.changeWorkspace");
		await new Promise(resolve => setTimeout(resolve, 3000));
		const typePicker = await eventuallyOk(() => { 
			const picker: vscode.QuickPick<vscode.QuickPickItem> = createQuickPick.getCall(0).returnValue;
			return picker;
		  }, 2000);
		  typePicker.selectedItems = await typePicker.items.filter(i => i.label === workspace_2.subscriptionName);
		  acceptQuickPick.fire();
		  await new Promise(resolve => setTimeout(resolve, 2000));

		  typePicker.selectedItems = await typePicker.items.filter(i => i.label === workspace_2.resourceGroup);
		  acceptQuickPick.fire();
		  await new Promise(resolve => setTimeout(resolve, 2000));

		  typePicker.selectedItems = await typePicker.items.filter(i => i.label === workspace_2.workspace);
		  acceptQuickPick.fire();
		  await new Promise(resolve => setTimeout(resolve, 2000));
		// pull workspace from azurequantumconfig.json and test against
		// expected workspace details
		const workspaceInfo = await findWorkspace();
		assert(workspaceInfo["subscriptionId"],workspace_2["subscriptionId"]);
		assert(workspaceInfo["resourceGroup"],workspace_2["resourceGroup"]);
		assert(workspaceInfo["workspace"],workspace_2["workspace"]);
		assert(workspaceInfo["location"],workspace_2["location"]);
		clearStubsQuickPick();
	});

	it ("Get job results from button",async ()=>{
		// test the results button on the local tree view panel
		await commands.executeCommand("quantum-jobs.jobResultsButton", mockLocalPanelJob);
		await new Promise(resolve => setTimeout(resolve, 5000));
		const results = window.activeTextEditor?.document.getText();
		const testResults = JSON.stringify(expectedResult_1,null, 4);
		assert.equal(results, testResults);
	});

	it ("Get job details",async ()=>{
		// test the details button on the local tree view panel
		await commands.executeCommand("quantum-jobs.jobDetails", mockLocalPanelJob);
		await new Promise(resolve => setTimeout(resolve, 5000));
		const results = window.activeTextEditor?.document.getText();
		if(!results){
			throw Error;
		}
		// remove inputDataUri and outputDataUri as links will differ 
		// between calls
		const detailsJson = JSON.parse(results);
		delete detailsJson.inputDataUri;
		delete detailsJson.outputDataUri;
		const detailsString = JSON.stringify(detailsJson,null, 4);
		const expectedDetailsString = JSON.stringify(expectedDetails_1,null, 4);

		assert.equal(detailsString, expectedDetailsString);
	});

	it ("Get job results with Id",async ()=>{
		prepareStubsGetJobPalette(jobId_1);
		// test the results command from the palette
		await commands.executeCommand("quantum.jobResultsPalette");
		await new Promise(resolve => setTimeout(resolve, 5000));
		const results = window.activeTextEditor?.document.getText();
		const testResults = JSON.stringify(expectedResult_1,null, 4);
		assert.equal(results, testResults);
		clearStubsGetJobPalette();
	});

	// TODO CAN ONLY ENTER QUCKPICK CHOICES FOR PROVIDER AND TARGET
	// 1) IDEAL OPTION IS TO FIGURE OUT A WAY TO SET UP A SINON 
	// CREATEINPUTBOX FOR NAME AND ARGUMENTS INPUTS
	// 2) IF CANT FIGURE OUT SINON, CONSIDER MOVING SUBMIT JOB 
	// FUNCTIONALITY (BUILDING AND RUNNING THE EXECUTABLE) WITH HARDCODED
	// PAREMETERS 
	it("Submit job to Azure Quantum", async ()=>{
		prepareStubsQuickPick();
		await commands.executeCommand("quantum.submitJob");
		await new Promise(resolve => setTimeout(resolve, 2000));

		const typePicker = await eventuallyOk(() => { 
			const picker: vscode.QuickPick<vscode.QuickPickItem> = createQuickPick.getCall(0).returnValue;
			return picker;
		  }, 2000);
		  
		await new Promise(resolve => setTimeout(resolve, 3000));

		  typePicker.selectedItems = await typePicker.items.filter(i => i.label === jobParameters.provider);
		  acceptQuickPick.fire();
		  await new Promise(resolve => setTimeout(resolve, 2000));

		  typePicker.selectedItems = await typePicker.items.filter(i => i.label === jobParameters.target);
		  acceptQuickPick.fire();
		  await new Promise(resolve => setTimeout(resolve, 2000));
		  clearStubsQuickPick();

		  // TODO CREATEINPUTBOX PARAMETERS FOR JOB NAME AND JOB ARGUMENTS
		  // BELOW CODE IS SIMPLIFIED VERSION OF WHAT IS NEEDED TO SUBMIT JOB
		  sinon.stub(window, 'createInputBox').resolves(jobParameters.name); 
		  sinon.stub(window, 'createInputBox').resolves(jobParameters.additionalArgs); 

	});

	function prepareStubsGetJobPalette(jobId: string){
		// restore sinon
		sinon.restore();
		// prepare stubs
		sinon.stub(window, 'showInputBox').resolves(jobId);
	}

	function clearStubsGetJobPalette(){
		(window['showInputBox'] as any).restore();
	}

	function prepareStubsQuickPick(){
		const originalQuickPick = vscode.window.createQuickPick;
		createQuickPick = sinon.stub(vscode.window, 'createQuickPick').callsFake(() => {
		  const picker = originalQuickPick();
		  acceptQuickPick = new EventEmitter<void>();
		  sinon.stub(picker, 'onDidAccept').callsFake(acceptQuickPick.event);
		  return picker;
		});
	}

	function clearStubsQuickPick(){
		createQuickPick.restore();
	}

});