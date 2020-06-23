declare class DecompressZip {
    constructor(path : string);
    on(event : 'error', callback : (err : any) => void) : void;
    on(event : 'extract', callback : (log : any) => void) : void;
    on(event : 'progress', callback : (fileIndex : number, fileCount : number) => void) : void;
    extract(options : {
        path? : string,
        follow? : boolean,
        filter? : (file : string) => boolean,
        strip? : number,
        restrict?: boolean
    }) : void;
}

declare module 'decompress-zip' {
    export = DecompressZip;
}