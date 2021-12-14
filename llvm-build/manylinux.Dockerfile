FROM quay.io/pypa/manylinux_2_24_x86_64 as llvm-builder

RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    apt-transport-https \
    ca-certificates \
    ninja-build \
    sudo \
    software-properties-common \
    wget \
    && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# ccache in apt sources is too old and errors out.
# build it from source
WORKDIR /tmp
RUN git clone --depth 1 https://github.com/ccache/ccache -b v4.4.2 && \
    cmake -B /tmp/ccache/build -G Ninja -DCMAKE_BUILD_TYPE=Release -DZSTD_FROM_INTERNET=ON -DREDIS_STORAGE_BACKEND=OFF -DENABLE_TESTING=OFF /tmp/ccache && \
    cd /tmp/ccache/build && \
    ninja install && \
    rm -rf /tmp/build
WORKDIR /tmp

ENV LLVM_CMAKEFILE ""
ENV LLVM_DIR ""
ENV LLVM_INSTALL_DIR ""
ENV CCACHE_DIR ""
ENV CCACHE_CONFIGPATH ""
ENV PKG_NAME ""
ENV SOURCE_DIR ""
ENV CMAKE_FLAGS ""
ARG USERNAME=vsts
ARG USER_UID=1000
ARG USER_GID=$USER_UID
ARG LLVM_BUILD_DIR=/home/${USERNAME}/work/1/s/external/llvm-project/build

RUN groupadd --gid $USER_GID $USERNAME
RUN useradd -s /bin/bash --uid $USER_UID --gid $USERNAME -m $USERNAME
RUN echo $USERNAME ALL=\(root\) NOPASSWD:ALL > /etc/sudoers.d/$USERNAME
RUN chmod 0440 /etc/sudoers.d/$USERNAME

RUN echo "#!/bin/bash" >> /tmp/entrypoint.sh && \
    echo "echo \"set -e\"" >> /tmp/entrypoint.sh && \
    echo "set -e" >> /tmp/entrypoint.sh && \
    echo "echo \"([ ! -z \"\${LLVM_INSTALL_DIR}\" ] && sudo chown -R ${USER_UID}:${USER_GID} \${LLVM_INSTALL_DIR}) || [ -z \"\${LLVM_INSTALL_DIR}\" ]\"" >> /tmp/entrypoint.sh && \
    echo "([ ! -z \"\${LLVM_INSTALL_DIR}\" ] && sudo chown -R ${USER_UID}:${USER_GID} \${LLVM_INSTALL_DIR}) || [ -z \"\${LLVM_INSTALL_DIR}\" ]" >> /tmp/entrypoint.sh && \
    echo "echo \"sudo chown -R ${USER_UID}:${USER_GID} \${LLVM_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "sudo chown -R ${USER_UID}:${USER_GID} \${LLVM_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"sudo chown -R ${USER_UID}:${USER_GID} \${CCACHE_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "sudo chown -R ${USER_UID}:${USER_GID} \${CCACHE_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"sudo chown -R ${USER_UID}:${USER_GID} \${SOURCE_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "sudo chown -R ${USER_UID}:${USER_GID} \${SOURCE_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"cmake -G Ninja -C \${LLVM_CMAKEFILE} \${CMAKE_FLAGS} \${LLVM_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "cmake -G Ninja -C \${LLVM_CMAKEFILE} \${CMAKE_FLAGS} \${LLVM_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"ninja package\"" >> /tmp/entrypoint.sh && \
    echo "ninja package" >> /tmp/entrypoint.sh && \
    echo "echo \"([ ! -z \"\${LLVM_INSTALL_DIR}\" ] && ninja install) || [ -z \"\${LLVM_INSTALL_DIR}\" ]\"" >> /tmp/entrypoint.sh && \
    echo "([ ! -z \"\${LLVM_INSTALL_DIR}\" ] && ninja install) || [ -z \"\${LLVM_INSTALL_DIR}\" ]" >> /tmp/entrypoint.sh && \
    chmod +x /tmp/entrypoint.sh && \
    chown ${USER_UID}:${USER_GID} /tmp/entrypoint.sh

ENTRYPOINT ["bash", "-c", "/tmp/entrypoint.sh $*", "--"]

USER ${USER_UID}:${USER_GID}
WORKDIR ${LLVM_BUILD_DIR}
