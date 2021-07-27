FROM quay.io/pypa/manylinux2014_x86_64 as llvm-builder

RUN yum install -y ninja-build
ARG LLVM_VERSION=11.1.0
WORKDIR /tmp
#todo: verify both sigs
RUN curl -SsL https://github.com/llvm/llvm-project/releases/download/llvmorg-${LLVM_VERSION}/llvm-${LLVM_VERSION}.src.tar.xz -o llvm-${LLVM_VERSION}.src.tar.xz && \
    tar -xp -f ./llvm-${LLVM_VERSION}.src.tar.xz
WORKDIR /tmp/llvm-${LLVM_VERSION}.src
RUN mkdir build
WORKDIR /tmp/llvm-${LLVM_VERSION}.src/build
RUN cmake -G Ninja -DCMAKE_BUILD_TYPE=Release ..
RUN ninja all
RUN ninja install
RUN cmake -DCMAKE_INSTALL_PREFIX=/tmp/llvm-install -P cmake_install.cmake

WORKDIR /tmp
RUN curl -SsL https://github.com/llvm/llvm-project/releases/download/llvmorg-${LLVM_VERSION}/clang-${LLVM_VERSION}.src.tar.xz -o clang-${LLVM_VERSION}.src.tar.xz && \
    tar -xp -f ./clang-${LLVM_VERSION}.src.tar.xz
WORKDIR /tmp/clang-${LLVM_VERSION}.src
RUN mkdir build
WORKDIR /tmp/clang-${LLVM_VERSION}.src/build
RUN cmake -G Ninja -DCMAKE_BUILD_TYPE=Release ..
RUN ninja all
RUN ninja install
RUN cmake -DCMAKE_INSTALL_PREFIX=/tmp/clang-install -P cmake_install.cmake

FROM quay.io/pypa/manylinux2014_x86_64 as manylinux2014_x86_64_llvm

COPY --from=llvm-builder /tmp/llvm-install/ /usr/lib/llvm-11/
COPY --from=llvm-builder /tmp/clang-install/ /usr/lib/llvm-11/

FROM quay.io/pypa/manylinux2014_x86_64 as builder

ENV PATH /root/.cargo/bin:$PATH

# todo, lock down version
RUN curl --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y

WORKDIR /tmp
RUN curl -SsL https://github.com/PyO3/maturin/archive/refs/tags/v0.11.1.tar.gz -o v0.11.1.tar.gz && \
    tar -xz -f ./v0.11.1.tar.gz

RUN mv ./maturin-0.11.1 /maturin

# Manually update the timestamps as ADD keeps the local timestamps and cargo would then believe the cache is fresh
RUN touch /maturin/src/lib.rs /maturin/src/main.rs

RUN cargo rustc --bin maturin --manifest-path /maturin/Cargo.toml --release -- -C link-arg=-s \
    && mv /maturin/target/release/maturin /usr/bin/maturin \
    && rm -rf /maturin

FROM manylinux2014_x86_64_llvm

ENV PATH /root/.cargo/bin:$PATH
# Add all supported python versions
ENV PATH /opt/python/cp36-cp36m/bin/:/opt/python/cp37-cp37m/bin/:/opt/python/cp38-cp38/bin/:/opt/python/cp39-cp39/bin/:$PATH
# Otherwise `cargo new` errors
ENV USER root

RUN curl --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y \
    && python3 -m pip install --no-cache-dir cffi \
    && mkdir /io

COPY --from=builder /usr/bin/maturin /usr/bin/maturin

WORKDIR /io

RUN yum install -y libffi-devel

ENTRYPOINT ["/usr/bin/maturin"]
