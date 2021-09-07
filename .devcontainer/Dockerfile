# See here for image contents: https://github.com/microsoft/vscode-dev-containers/blob/v0.191.0/containers/codespaces-linux/.devcontainer/base.Dockerfile

FROM mcr.microsoft.com/vscode/devcontainers/universal:1-linux

USER root

# Install CMake 3.20 (required since apt-get uses 3.16 and repo requires 3.20)
RUN curl -SsL https://github.com/Kitware/CMake/releases/download/v3.20.5/cmake-3.20.5-linux-x86_64.sh -o cmakeinstall.sh \
    && echo "f582e02696ceee81818dc3378531804b2213ed41c2a8bc566253d16d894cefab cmakeinstall.sh" | sha256sum -c --strict - \
    && chmod +x cmakeinstall.sh \
    && ./cmakeinstall.sh --prefix=/usr/local --exclude-subdir \
    && rm cmakeinstall.sh

RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get install -y --no-install-recommends ninja-build clang-11 clang-tidy-11 build-essential \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/

USER codespace
