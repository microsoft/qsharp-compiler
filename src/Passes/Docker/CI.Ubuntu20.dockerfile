FROM ubuntu:20.04

# basic dependencies -
ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update -y && \
  apt-get install -y 

RUN apt-get install -y curl \
  pkg-config \
  findutils \
  wget

# build dependencies
RUN   apt install -y clang-11 cmake clang-format-11 clang-tidy-11 && \
      apt-get install -y llvm-11 lldb-11 llvm-11-dev libllvm11 llvm-11-runtime && \
      export CC=clang-11 && \
      export CXX=clang++ 

# Python
RUN apt install -y python3 python3-pip && \
    update-alternatives --install /usr/bin/python python /usr/bin/python3 0

ADD . /src/
RUN cd /src/ && \
      pip install -r requirements.txt && \
      chmod +x manage

# running the build
ENV CC=clang-11 \
  CXX=clang++-11 \
  PYTHONUNBUFFERED=1 \
  PYTHON_BIN_PATH=/usr/bin/python3

WORKDIR /src/
ENV  export CC=clang-11   \
     export CXX=clang++-11  

