FROM mcr.microsoft.com/vscode/devcontainers/rust:1

COPY ./llvm.sh .
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive && \
    apt-get install -y --no-install-recommends lsb-release wget software-properties-common python3-pip && \
    python3 -m pip install -U pip && \
    chmod +x llvm.sh && \
    ./llvm.sh 11 && \
    rm llvm.sh

# [Optional] Uncomment this section to install additional packages.
# RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
#     && apt-get -y install --no-install-recommends <your-package-list-here>
