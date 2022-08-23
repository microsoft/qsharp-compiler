import * as vscode from "vscode";
import * as glob from "glob";

export async function findWorkspace(){
	let rootFolder= "";
	let pullConfigFiles:any[] =[];
	if(vscode?.workspace?.workspaceFolders){
	  rootFolder = vscode?.workspace?.workspaceFolders[0]?.uri.fsPath;
	}
	await new Promise<void>(async (resolve)=>{
	  await glob('**/azurequantumconfig.json', { cwd: rootFolder }, (err, files) => {
		if (err) {
		  console.log(err);
		  resolve();
		}
		pullConfigFiles = files;
		resolve();
	  });
  });

  const workspaceInfoChunk: any = await vscode.workspace.fs.readFile(
	  vscode.Uri.file(rootFolder+"/"+pullConfigFiles[0])
	);
	return JSON.parse(String.fromCharCode(...workspaceInfoChunk));
}


export const eventuallyOk = async <T>(
	fn: () => Promise<T> | T,
	timeout = 10000,
	wait = 500,
  ): Promise<T> => {
	const deadline = Date.now() + timeout;
	while (true) {
	  try {
		return await fn();
	  } catch (e) {
		if (Date.now() + wait > deadline) {
		  throw e;
		}
  
		await delay(wait);
	  }
	}
  };
  export const delay = (duration: number) =>
  isFinite(duration)
    ? new Promise<void>(resolve => setTimeout(resolve, duration))
    : new Promise<void>(() => undefined);
