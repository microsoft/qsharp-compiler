#!/bin/bash 
set -x
set -e

. ./set-env.sh

##
# Q# compiler
##
pack_one() {
    nuget pack $1 \
        -OutputDirectory $NUGET_OUTDIR \
        -Properties Configuration=$BUILD_CONFIGURATION \
        -Version $NUGET_VERSION \
        -Verbosity detailed \
        $2

}

echo "##[info]Using nuget to create packages"
pack_one ../src/QsCompiler/Compiler/QsCompiler.csproj -IncludeReferencedProjects

##
# VSCode extension
##
pushd ../src/VSCodeExtension
if [ -f 'package.json' ]; then
    npm install vsce
    vsce package
else
    echo "
    ---------------------------------------------------------------------------
    Missing package.json. vs-code extension will be skipped.
    To build it, execute setup.ps1 first.
    ---------------------------------------------------------------------------"
fi
popd