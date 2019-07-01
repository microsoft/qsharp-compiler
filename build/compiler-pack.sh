#!/bin/bash 
set -x
set -e

. ./set-env.sh

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
