import * as assert from 'assert';
import * as path from "path";
import * as vscode from 'vscode';
import * as sinon from 'sinon';
import * as cp from 'child_process';
import { promisify } from 'util';
//import * as extension from '../../extension';

suite('Extension Test Suite', () => {
	vscode.window.showInformationMessage('Start all tests.');

	test("Should start quantum extension", async () => {
		
		const started = vscode.extensions.getExtension(
			"quantum.quantum-devkit-vscode",
		);

		await started?.activate();

		assert.notEqual(started, undefined);
		assert.equal(started?.isActive, true);

	});

	test("Should register all four commands", async () => {
		
		const commandsList = await vscode.commands.getCommands();
		const quantumCommandsList = commandsList.filter(x => x.startsWith("quantum"));

		assert.equal(quantumCommandsList.includes("quantum.newProject"), true );
		assert.equal(quantumCommandsList.includes("quantum.installTemplates"), true );
		assert.equal(quantumCommandsList.includes("quantum.openDocumentation"), true );
		assert.equal(quantumCommandsList.includes("quantum.installIQSharp"), true );

	});

	test("Should create a standalone console application and run it successfully", async () => {
		// prepare stubs
		sinon.stub(vscode.window, 'showQuickPick').resolves( Object('Standalone console application'));
		
		let projectPath  = path.join(__dirname, '/test/application')
		let uri = vscode.Uri.file(projectPath);
		sinon.stub(vscode.window, 'showSaveDialog').resolves(uri);
		
		// run the newProject vsc command
		await vscode.commands.executeCommand("quantum.newProject");
		
		// dotnet run
		let results = await promisify(cp.exec)("dotnet run --project " + projectPath);
		
		//assert
		assert.equal(results.stderr, "");
		assert.equal(results.stdout, "Hello quantum world!\r\n");
		
		//TODO: clear stubs?
		(vscode.window['showQuickPick'] as any).restore();
		(vscode.window['showSaveDialog'] as any).restore(); 

	});

	test("Should create a library project and build it successfully", async () => {
		// prepare stubs
		sinon.stub(vscode.window, 'showQuickPick').resolves( Object('Quantum library'));
		
		let projectPath  = path.join(__dirname, '/test/library')
		let uri = vscode.Uri.file(projectPath);
		sinon.stub(vscode.window, 'showSaveDialog').resolves(uri);
		
		// run the newProject vsc command
		await vscode.commands.executeCommand("quantum.newProject");
		
		// run the project
		let results = await promisify(cp.exec)("dotnet build " + projectPath);
		
		//assert
		assert.equal(results.stderr, "");
		assert.notEqual(results.stdout.indexOf('Build succeeded'), -1);
		
		//TODO: clear stubs?
		(vscode.window['showQuickPick'] as any).restore();
		(vscode.window['showSaveDialog'] as any).restore(); 

	});

	test("Should create a unit test project and run the test successfully", async () => {
		// prepare stubs
		sinon.stub(vscode.window, 'showQuickPick').resolves( Object('Unit testing project'));
		
		let projectPath  = path.join(__dirname, '/test/unittest')
		let uri = vscode.Uri.file(projectPath);
		sinon.stub(vscode.window, 'showSaveDialog').resolves(uri);
		
		// run the newProject vsc command
		await vscode.commands.executeCommand("quantum.newProject");
		
		// run the project
		let results = await promisify(cp.exec)("dotnet test " + projectPath);
		
		//assert
		assert.equal(results.stderr, "");
		assert.notEqual(results.stdout.indexOf('Passed!'), -1);
		
		//TODO: clear stubs?
		(vscode.window['showQuickPick'] as any).restore();
		(vscode.window['showSaveDialog'] as any).restore(); 

	});




});


