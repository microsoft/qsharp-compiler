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

FROM quay.io/pypa/manylinux2014_x86_64

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
