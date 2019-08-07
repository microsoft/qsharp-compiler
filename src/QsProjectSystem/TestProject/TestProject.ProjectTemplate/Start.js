WScript.Echo(GetExecutionInformation());
WScript.Echo("Done.");

function GetExecutionInformation() {
    var info = "";

    // Display the script name
    info += "Script Name: " + WScript.ScriptName + "\n";

    // Display the working directory
    var shell = WScript.CreateObject("WScript.Shell");
    info += "Current directory: " + shell.CurrentDirectory + "\n";

    // Display the set of arguments
    info += "Arguments (" + WScript.Arguments.length + "): \n";
    for (i = 0; i < WScript.Arguments.length; i++) {
        info += "\t[" + i + "]: " + WScript.Arguments.Item(i) + "\n";
    }

    return info;
}
