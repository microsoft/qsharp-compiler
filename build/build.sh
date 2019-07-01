#!/bin/bash 
set -x
set -e

. ./set-env.sh

./compiler-build.sh

powershell -NoProfile ./extensions-build.ps1 \
    $BUILD_CONFIGURATION \
    $ASSEMBLY_VERSION \
    $ASSEMBLY_CONSTANTS
