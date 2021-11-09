##########################
# Generator
##########################
FROM ubuntu:20.04 as generator

ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update -y && \
  apt-get install -y 

RUN apt-get install -y curl \
  pkg-config \
  findutils \
  wget \
  unzip doxygen

RUN wget https://github.com/matusnovak/doxybook2/releases/download/v1.3.6/doxybook2-linux-amd64-v1.3.6.zip && \
    unzip doxybook2-linux-amd64-v1.3.6.zip

ADD Source/ /build/Source/
ADD doxygen.cfg /build/

ADD docs/ /build/docs/

WORKDIR /build/
RUN doxygen doxygen.cfg && \
   doxybook2 --input Doxygen/xml --config docs/.doxybook/config.json --output docs/src/ && \
   rm -rf docs/src/Namespaces/namespace_0d* 

##########################
# Builder
##########################
FROM node:alpine as builder

RUN mkdir /src/
COPY --from=generator /build/docs/ /docs/

WORKDIR /docs/
RUN yarn install && yarn build


##########################
# Documentation
##########################
FROM nginx:latest as documentation
ADD docs/nginx/default.conf /etc/nginx/conf.d/default.conf

COPY --from=builder /docs/src/.vuepress/dist/ /usr/share/nginx/html
