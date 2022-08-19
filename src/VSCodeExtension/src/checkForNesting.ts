//  Verify there are not nested csproj files
export function checkForNesting(files:any[]){
    // all csproj files must have same depth
    const depth = files[0].path.split("/").length;
    let file:any;
    let fileNum:any;
    for(fileNum in files){
        file = files[fileNum];
        if (file.path.split("/").length !== depth){
            return true;
        }
        return false;
    }
}