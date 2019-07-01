#!/bin/bash 
set -x
set -e

. ./set-env.sh

./compiler-pack.sh

powershell -NoProfile ./extensions-pack.ps1 \
    $BUILD_CONFIGURATION \
    $ASSEMBLY_VERSION \
    $VSIX_OUTDIR
