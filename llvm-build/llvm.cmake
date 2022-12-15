# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

cmake_minimum_required(VERSION 3.4 FATAL_ERROR)

message(STATUS CMAKE_HOST_SYSTEM_NAME=${CMAKE_HOST_SYSTEM_NAME})

if (${CMAKE_HOST_SYSTEM_NAME} MATCHES "Windows")
  find_program(SCCACHE sccache)
  if(SCCACHE)
      set(LLVM_CCACHE_BUILD OFF CACHE BOOL "")
      set_property(GLOBAL PROPERTY RULE_LAUNCH_COMPILE "${SCCACHE}")
      get_property(rule_launch_property GLOBAL PROPERTY RULE_LAUNCH_COMPILE)
      message(STATUS RULE_LAUNCH_COMPILE=${rule_launch_property})
      set(CMAKE_C_COMPILER_LAUNCHER "${SCCACHE}" CACHE STRING "")
      message(STATUS CMAKE_C_COMPILER_LAUNCHER=${CMAKE_C_COMPILER_LAUNCHER})
      set(CMAKE_CXX_COMPILER_LAUNCHER "${SCCACHE}" CACHE STRING "")
      message(STATUS CMAKE_CXX_COMPILER_LAUNCHER=${CMAKE_CXX_COMPILER_LAUNCHER})
  else()
    message(STATUS "Not using sccache")
  endif()
else()
  # Prepare cache if we find it
  find_program(CCACHE ccache)
  if(CCACHE)
    message(STATUS "Using CCache for linux/darwin")
    set(LLVM_CCACHE_BUILD ON CACHE BOOL "")
    set(LLVM_CCACHE_DIR $ENV{CCACHE_DIR} CACHE STRING "")
    set(LLVM_CCACHE_MAXSIZE "2G" CACHE STRING "")
    message(STATUS CCACHE_DIR=$ENV{CCACHE_DIR})
    message(STATUS LLVM_CCACHE_BUILD=${LLVM_CCACHE_BUILD})
    message(STATUS LLVM_CCACHE_DIR=${LLVM_CCACHE_DIR})
    message(STATUS LLVM_CCACHE_MAXSIZE=${LLVM_CCACHE_MAXSIZE})
  else()
    message(STATUS "Not using CCache")
  endif()
endif()


set(CPACK_PACKAGE_FILE_NAME $ENV{PKG_NAME} CACHE STRING "")
message(STATUS CPACK_PACKAGE_FILE_NAME=${CPACK_PACKAGE_FILE_NAME})

# Set up main build props

set(CMAKE_BUILD_TYPE MinSizeRel CACHE STRING "")

set(LLVM_TARGETS_TO_BUILD "Native" CACHE STRING "")

set(PACKAGE_VENDOR LLVM.org CACHE STRING "")

# Turn off
set(LLVM_ENABLE_ASSERTIONS OFF CACHE BOOL "")
set(LLVM_BUILD_EXAMPLES OFF CACHE BOOL "")
set(LLVM_ENABLE_RTTI OFF CACHE BOOL "")

# Remove external lib dependencies
set(LLVM_ENABLE_BINDINGS OFF CACHE BOOL "")
set(LLVM_ENABLE_FFI OFF CACHE BOOL "")
set(LLVM_ENABLE_LIBEDIT OFF CACHE BOOL "")
set(LLVM_ENABLE_LIBPFM OFF CACHE BOOL "")
set(LLVM_ENABLE_LIBXML2 OFF CACHE BOOL "")
set(LLVM_ENABLE_OCAMLDOC OFF CACHE BOOL "")
set(LLVM_ENABLE_TERMINFO OFF CACHE BOOL "")
set(LLVM_ENABLE_ZLIB OFF CACHE BOOL "")
set(LLVM_ENABLE_ZSTD OFF CACHE BOOL "")

# Packing
set(CPACK_BINARY_DEB OFF CACHE BOOL "")
set(CPACK_BINARY_FREEBSD OFF CACHE BOOL "")
set(CPACK_BINARY_IFW OFF CACHE BOOL "")
set(CPACK_BINARY_NSIS OFF CACHE BOOL "")
set(CPACK_BINARY_RPM OFF CACHE BOOL "")
set(CPACK_BINARY_STGZ OFF CACHE BOOL "")
set(CPACK_BINARY_TBZ2 OFF CACHE BOOL "")
set(CPACK_BINARY_TXZ OFF CACHE BOOL "")
set(CPACK_BINARY_TZ OFF CACHE BOOL "")

if (${CMAKE_HOST_SYSTEM_NAME} MATCHES "Windows")
  message(STATUS "Configuring for Windows")
  set(LLVM_USE_CRT_RELEASE "MT" CACHE STRING "")
  set(LLVM_USE_CRT_MINSIZEREL "MT" CACHE STRING "")
  set(LLVM_BUILD_LLVM_C_DYLIB ON CACHE BOOL "")
  set(CMAKE_INSTALL_UCRT_LIBRARIES ON CACHE BOOL "")
  set(CPACK_BINARY_ZIP ON CACHE BOOL "")
else()
  set(LLVM_BUILD_LLVM_DYLIB ON CACHE BOOL "")
  set(CPACK_BINARY_TGZ ON CACHE BOOL "")
endif()

# Apple specific changes to match their toolchain
if(APPLE)
  set(COMPILER_RT_ENABLE_IOS OFF CACHE BOOL "")
  set(COMPILER_RT_ENABLE_WATCHOS OFF CACHE BOOL "")
  set(COMPILER_RT_ENABLE_TVOS OFF CACHE BOOL "")

  set(CMAKE_MACOSX_RPATH ON CACHE BOOL "")
  set(CLANG_SPAWN_CC1 ON CACHE BOOL "")
  set(CMAKE_C_FLAGS "-fno-stack-protector -fno-common -Wno-profile-instr-unprofiled" CACHE STRING "")
  set(CMAKE_CXX_FLAGS "-fno-stack-protector -fno-common -Wno-profile-instr-unprofiled" CACHE STRING "")
endif()

# See https://github.com/llvm/llvm-project/blob/llvmorg-14.0.6/llvm/utils/gn/build/write_library_dependencies.py for a list
# of these dependencies and what they bring into the linked binary.
set(LLVM_DYLIB_COMPONENTS "core;debuginfodwarf;linker;support;target;bitwriter;analysis;executionengine;runtimedyld;mcjit;bitstreamreader;bitreader;native" CACHE STRING "")
