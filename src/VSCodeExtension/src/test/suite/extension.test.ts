import * as assert from 'assert';
import * as path from "path";
import * as sinon from 'sinon';
import * as cp from 'child_process';
import * as vscode from 'vscode';
import { promisify } from 'util';
//import * as extension from '../../extension';

suite('Extension Test Suite', () => {
	vscode.window.showInformationMessage('Start all tests.');

	test("Should start quantum extension", async () => {
		// activate the extension
		const started = vscode.extensions.getExtension("quantum.quantum-devkit-vscode");
		await started?.activate();

		// assert
		assert.notEqual(started, undefined);
		assert.equal(started?.isActive, true);
	});

	test("Should register all four commands", async () => {
		// get commands
		const commandsList = await vscode.commands.getCommands();
		const quantumCommandsList = commandsList.filter(x => x.startsWith("quantum"));

		//assert
		assert.equal(quantumCommandsList.includes("quantum.newProject"), true );
		assert.equal(quantumCommandsList.includes("quantum.installTemplates"), true );
		assert.equal(quantumCommandsList.includes("quantum.openDocumentation"), true );
		assert.equal(quantumCommandsList.includes("quantum.installIQSharp"), true );
	});

	test("Should create a standalone console application and run it successfully", async () => {
		// prepare stubs
		const projectPath = path.join(__dirname, '/test/application');
		prepareStubs('Standalone console application', projectPath);

		// run the newProject command
		await vscode.commands.executeCommand("quantum.newProject");
		
		// dotnet run
		let results = await promisify(cp.exec)("dotnet run --project " + projectPath);
	
		// assert
		assert.equal(results.stderr, "");
		assert.equal(results.stdout, "Hello quantum world!\r\n");

		// clear stubs
		clearStubs();
	});

	test("Should create a library project and build it successfully", async () => {
		// prepare stubs
		const projectPath = path.join(__dirname, '/test/library');
		prepareStubs('Quantum library', projectPath);
		
		// run the newProject command
		await vscode.commands.executeCommand("quantum.newProject");
		
		// run the project
		let results = await promisify(cp.exec)("dotnet build " + projectPath);
		
		// assert
		assert.equal(results.stderr, "");
		assert.notEqual(results.stdout.indexOf('Build succeeded'), -1);
		
		// clear stubs
		clearStubs();
	});

	test("Should create a unit test project and run the test successfully", async () => {
		// prepare stubs
		const projectPath = path.join(__dirname, '/test/unittest');
		prepareStubs('Unit testing project', projectPath);

		// run the newProject command
		await vscode.commands.executeCommand("quantum.newProject");

		// run the project
		let results = await promisify(cp.exec)("dotnet test " + projectPath);
		
		// assert
		assert.equal(results.stderr, "");
		assert.notEqual(results.stdout.indexOf('Test Run Successful.'), -1);
		
		// clear stubs
		clearStubs();
	});

	test("Should provide code actions", async () => {
		// open document
		await openDocument('/test/unittest/Tests.qs');

		if (vscode.window.activeTextEditor == undefined || vscode.window.activeTextEditor.document == undefined){
			throw 'No document at the active text editor!';
		}
		
		// execute code action provider on the AllocateQubit operation
		const codeAction = await vscode.commands.executeCommand('vscode.executeCodeActionProvider', vscode.window.activeTextEditor.document.uri, new vscode.Selection(7, 15, 7, 20)) as vscode.CodeAction[];
		
		// close the active document
		await closeDocument();

		// assert
		assert.equal(codeAction[0]?.title?.includes('Add documentation for AllocateQubit.'), true);
	});

	test("Should provide type information when hovering", async () => {
		// open document
		await openDocument('/test/application/Program.qs');

		if (vscode.window.activeTextEditor == undefined || vscode.window.activeTextEditor.document == undefined){
			throw 'No document at the active text editor!';
		}

		// execute hover provider on the Unit type
		const hover = await vscode.commands.executeCommand('vscode.executeHoverProvider', vscode.window.activeTextEditor.document.uri, new vscode.Position(6, 30)) as vscode.Hover[];
		
		// close the active document
		await closeDocument();
		
		// assert
  		assert.equal((hover[0]?.contents as vscode.MarkdownString[])[0]?.value?.includes('Built-in type Unit'), true);
	});

	test("Should provide language diagnostics", async () => {
		// open document
		await openDocument('/test/application/Program.qs');

		if (vscode.window.activeTextEditor == undefined || vscode.window.activeTextEditor.document == undefined){
			throw 'No document at the active text editor!';
		}
		
		let originalContent = await vscode.window.activeTextEditor?.document.getText();
		let originalDiagnostics = await vscode.languages.getDiagnostics(vscode.window.activeTextEditor.document.uri);
		
		// update the document with invalid text
		let updatedContent = originalContent + 'invalid_text';
		let fullRange = vscode.window.activeTextEditor.document.validateRange(new vscode.Range(0, 0, vscode.window.activeTextEditor.document.lineCount, 0));
		
		await vscode.window.activeTextEditor.edit(editBuilder => editBuilder.replace(fullRange, updatedContent));
		await vscode.window.activeTextEditor.document.save();
		await sleep(5000);

		let updatedDiagnostics = await vscode.languages.getDiagnostics(vscode.window.activeTextEditor.document.uri);

		// revert the document content back to original
		fullRange = vscode.window.activeTextEditor.document.validateRange(new vscode.Range(0, 0, vscode.window.activeTextEditor.document.lineCount, 0));
		await vscode.window.activeTextEditor.edit(editBuilder => editBuilder.replace(fullRange, originalContent));
		await vscode.window.activeTextEditor.document.save();

		// close the active document
		await closeDocument();

		// assert
		assert.equal(originalDiagnostics.length, 0);
		assert.equal(updatedDiagnostics[0].message.includes('An expression used as a statement must be a call expression.'), true);

	});

	test("Should provide auto-complete functionality", async () => {
		// open document
		await openDocument('/test/application/Program.qs');

		if (vscode.window.activeTextEditor == undefined || vscode.window.activeTextEditor.document == undefined){
			throw 'No document at the active text editor!';
		}

		// execute the completion provider
		const completion = await vscode.commands.executeCommand('vscode.executeCompletionItemProvider', vscode.window.activeTextEditor.document.uri, new vscode.Position(6, 29)) as vscode.CompletionList;
		
		// close the active document
		await closeDocument();
		
		// assert
		assert.notEqual(completion, undefined);
		assert.notEqual(completion.items.length, 0);
		assert.notEqual(completion.items.filter(e => e.label === 'Unit'), 0);
	});

	function sleep(ms: number) {
		return new Promise(resolve => setTimeout(resolve, ms));
	}

	async function openDocument(filePath: string){
		let uri = vscode.Uri.file(path.join(__dirname, filePath));
		await vscode.workspace.openTextDocument(uri).then(document => vscode.window.showTextDocument(document));
		// waiting for the language server
		await sleep(5000);
	}

	async function closeDocument(){
		await vscode.commands.executeCommand('workbench.action.closeActiveEditor');
	}

	function prepareStubs(option: string, folderPath: string){
		// restore sinon
		sinon.restore();
		
		// prepare stubs
		sinon.stub(vscode.window, 'showQuickPick').resolves(Object(option));
		sinon.stub(vscode.window, 'showSaveDialog').resolves(vscode.Uri.file(folderPath));
	}

	function clearStubs(){
		(vscode.window['showQuickPick'] as any).restore();
		(vscode.window['showSaveDialog'] as any).restore(); 
	}

});