# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

python --version
pip --version
virtualenv --version

# Installing requirements for CI
cd src/Passes
pip install -r requirements.txt

# Running CI
python manage runci


throw "Preventing other steps from running."
