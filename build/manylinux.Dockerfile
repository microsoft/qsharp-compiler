FROM quay.io/pypa/manylinux2014_x86_64 as llvm-builder

RUN yum install -y ninja-build ccache sudo

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
    echo "echo \"[ -v LLVM_INSTALL_DIR ] && sudo chown -R 1000:1000 \${LLVM_INSTALL_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "[ -v LLVM_INSTALL_DIR ] && sudo chown -R 1000:1000 \${LLVM_INSTALL_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"sudo chown -R 1000:1000 \${LLVM_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "sudo chown -R 1000:1000 \${LLVM_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"sudo chown -R 1000:1000 \${CCACHE_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "sudo chown -R 1000:1000 \${CCACHE_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"sudo chown -R 1000:1000 \${SOURCE_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "sudo chown -R 1000:1000 \${SOURCE_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"cmake -G Ninja -C \${LLVM_CMAKEFILE} \${CMAKE_FLAGS} \${LLVM_DIR}\"" >> /tmp/entrypoint.sh && \
    echo "cmake -G Ninja -C \${LLVM_CMAKEFILE} \${CMAKE_FLAGS} \${LLVM_DIR}" >> /tmp/entrypoint.sh && \
    echo "echo \"ninja package\"" >> /tmp/entrypoint.sh && \
    echo "ninja package" >> /tmp/entrypoint.sh && \
    echo "echo \"[ -v LLVM_INSTALL_DIR ] && ninja install\"" >> /tmp/entrypoint.sh && \
    echo "[ -v LLVM_INSTALL_DIR ] && ninja install" >> /tmp/entrypoint.sh && \
    chmod +x /tmp/entrypoint.sh && \
    chown ${USER_UID}:${USER_GID} /tmp/entrypoint.sh

ENTRYPOINT ["sh", "-c", "/tmp/entrypoint.sh $*", "--"]

USER ${USER_UID}:${USER_GID}
WORKDIR ${LLVM_BUILD_DIR}
