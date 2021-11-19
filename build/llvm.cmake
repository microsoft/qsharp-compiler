# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT License.

cmake_minimum_required(VERSION 3.4 FATAL_ERROR)

set(LLVM_ENABLE_PROJECTS "clang;clang-tools-extra;lld" CACHE STRING "")
set(LLVM_ENABLE_RUNTIMES "compiler-rt;libcxx;libcxxabi" CACHE STRING "")

message(STATUS CMAKE_HOST_SYSTEM_NAME=${CMAKE_HOST_SYSTEM_NAME})

if (${CMAKE_HOST_SYSTEM_NAME} MATCHES "Windows")
  find_program(SCCACHE sccache)
  if(SCCACHE)
      set(LLVM_CCACHE_BUILD OFF CACHE BOOL "")
      set_property(GLOBAL PROPERTY RULE_LAUNCH_COMPILE "${SCCACHE}")
      message(STATUS RULE_LAUNCH_COMPILE=${RULE_LAUNCH_COMPILE})
      set(CMAKE_C_COMPILER_LAUNCHER "sccache" CACHE STRING "")
      set(CMAKE_CXX_COMPILER_LAUNCHER "sccache" CACHE STRING "")
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

# LLDB
#set(LLDB_ENABLE_LIBEDIT OFF CACHE BOOL "")
#set(LLDB_ENABLE_CURSES OFF CACHE BOOL "")
#set(LLDB_ENABLE_LIBXML2 OFF CACHE BOOL "")
#set(LLDB_ENABLE_PYTHON OFF CACHE BOOL "")
#set(LLDB_ENABLE_LUA OFF CACHE BOOL "")
#
#if (APPLE)
#  # Fixing: Development code sign identity not found: 'lldb_codesign'
#  # This will cause failures in the test suite.
#  # Usd the system one instead. See 'Code Signing on macOS' in the documentation.
#  set(LLDB_USE_SYSTEM_DEBUGSERVER ON CACHE BOOL "")
#  set(LLDB_INCLUDE_TESTS OFF CACHE BOOL "")
#endif()

set(CMAKE_BUILD_TYPE MinSizeRel CACHE STRING "")

set(LLVM_TARGETS_TO_BUILD "X86" CACHE STRING "")

set(PACKAGE_VENDOR LLVM.org CACHE STRING "")

# Turn off
set(LLVM_ENABLE_ASSERTIONS OFF CACHE BOOL "")
set(LLVM_BUILD_EXAMPLES OFF CACHE BOOL "")
set(LLVM_ENABLE_RTTI OFF CACHE BOOL "")

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
