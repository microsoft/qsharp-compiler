#!/bin/bash 
set -x
set -e

. ./set-env.sh

test_one() {
    dotnet test $1 \
        -c $BUILD_CONFIGURATION \
        -v $BUILD_VERBOSITY \
        --logger trx \
        /property:DefineConstants=$ASSEMBLY_CONSTANTS \
        /property:Version=$ASSEMBLY_VERSION
}

echo "##[info]Testing C# code generation"
test_one '../QsCompiler.sln'

