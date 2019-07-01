#!/bin/bash 
set -x
set -e

. ./set-env.sh


do_one() {
    dotnet $1 $2 \
        -c $BUILD_CONFIGURATION \
        -v $BUILD_VERBOSITY \
        /property:DefineConstants=$ASSEMBLY_CONSTANTS \
        /property:Version=$ASSEMBLY_VERSION 
}

echo "##[info]Build Q# compiler"
do_one build '../QsCompiler.sln'

echo "##[info]Publish Q# compiler app"
do_one publish '../src/QsCompiler/CommandLineTool/QsCommandLineTool.csproj'

echo "##[info]Publish Q# Language Server"
do_one publish '../src/QsCompiler/LanguageServer/QsLanguageServer.csproj'


