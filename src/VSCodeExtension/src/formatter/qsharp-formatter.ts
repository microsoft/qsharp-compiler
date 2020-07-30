import * as fs from "fs";
import * as cp from 'child_process';
import {
    CancellationToken,
    Disposable,
    DocumentFormattingEditProvider,
    EndOfLine,
    FormattingOptions,
    Range,
    TextDocument,
    TextEdit,
    window,
    workspace,
    OutputChannel
} from "vscode";
import { promisify } from "util";

export default class QSharpFormatter
    implements
    Disposable,
    DocumentFormattingEditProvider {
    private disposables: Disposable[] = [];
    private keepCRLF: boolean = false;
    private additionalFlags: string = "";
    private qsCompilerCommand: string = "qsc";
    private showErrorNotification: boolean = false;

    private outputChannel: OutputChannel = window.createOutputChannel("Q# Formatter");

    constructor() {
        this.loadSettings();
        this.disposables.push(this.outputChannel);
        workspace.onDidChangeConfiguration(
            (evt) => {
                if (evt.affectsConfiguration("quantumDevKit")) {
                    this.loadSettings();
                }
            },
            this,
            this.disposables
        );
    }

    public dispose(): void {
        this.disposables.forEach((i) => i.dispose());
    }

    public async provideDocumentFormattingEdits(
        document: TextDocument,
        options: FormattingOptions,
        token: CancellationToken
    ): Promise<TextEdit[]> {
        const edits: TextEdit[] = [];

        // if document has CRLF line endings, change it to LF.
        if (!this.keepCRLF && document.eol === EndOfLine.CRLF) {
            edits.push(TextEdit.setEndOfLine(EndOfLine.LF));
        }

        const fullRange: Range = this.fullDocumentRange(document);
        const formatted: string = await this.runQsharpFormatter(document);
        edits.push(TextEdit.replace(fullRange, formatted));
        return edits;
    }

    private loadSettings(): void {
        const config: any = workspace.getConfiguration("quantumDevKit");
        this.keepCRLF = config.keepCRLF;
        this.additionalFlags = config.additionalFlags;
        this.qsCompilerCommand = config.qsCompilerpath;
        this.showErrorNotification = config.showErrorNotification;
    }

    private async runQsharpFormatter(input: TextDocument): Promise<string> {
        let cmdName: string = this.qsCompilerCommand;
        const args: string[] = [];

        if (this.additionalFlags.length > 0) {
            args.push(...this.additionalFlags.split(" "));
        }

        const tmpDir = "C:\\Users\\t-rkoh\\Documents\\Projects\\qsharp-compiler\\src\\VSCodeExtension\\src\\tests\\tmp";
        const tmpFile = tmpDir + "\\" + input.fileName.replace(/^.*[\\\/]/, '');

        args.push("--input", input.uri.fsPath);
        args.push("--output", tmpDir);
        
        const command: string = `${cmdName} format ${args.join(' ')}`;

        const { stderr } = await promisify(cp.exec)(command);
        const err: string = String(stderr);
        if (err.length > 0) {
            this.outputChannel.appendLine(err);
            if (this.showErrorNotification) {
                window.showErrorMessage("Q# Formatter encountered some errors; see output for details. " + err);
            }
        }

        const formattedCode: string = await promisify(fs.readFile)(tmpFile, 'utf8');
        try {
            await promisify(fs.unlink)(tmpFile);
            fs.rmdirSync(tmpDir);
        } catch (err) {
            this.outputChannel.appendLine(err);
            if (this.showErrorNotification) {
                window.showErrorMessage("Q# Formatter encountered some errors; see output for details. " + err);
            }
        }
        
        return formattedCode;
    }

    private fullDocumentRange(document: TextDocument): Range {
        const last: number = document.lineCount - 1;
        return new Range(0, 0, last, document.lineAt(last).text.length);
    }
}
