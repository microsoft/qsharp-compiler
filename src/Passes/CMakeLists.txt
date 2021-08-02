cmake_minimum_required(VERSION 3.4.3)

project(QSharpPasses)

find_package(LLVM REQUIRED CONFIG)
include(CheckCXXCompilerFlag)

message(STATUS "Found LLVM ${LLVM_PACKAGE_VERSION}")
message(STATUS "Using LLVMConfig.cmake in: ${LLVM_DIR}")

# Setting the standard configuration for the C++ compiler
# Rather than allowing C++17, we restrict ourselves to 
# C++14 as this is the standard currently used by the LLVM 
# project for compilation of the framework. While there is 
# a very small chance that the difference in standard 
# would break things, it is a possibility nonetheless.
set(CMAKE_CXX_STANDARD 14)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wextra -Weverything -Wconversion -Wno-c++98-compat-pedantic -Wno-c++98-compat -Wno-padded -Wno-exit-time-destructors -Wno-global-constructors")
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Werror ")

# LLVM is normally built without RTTI. Be consistent with that.
if(NOT LLVM_ENABLE_RTTI)
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fno-rtti")
endif()

# We export the compile commands which are needed by clang-tidy 
# to run the static analysis
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Adding LLVM include directories. We may choose
# to move this to a module level at a later point
include_directories(${LLVM_INCLUDE_DIRS})
link_directories(${LLVM_LIBRARY_DIRS})
add_definitions(${LLVM_DEFINITIONS})
include_directories(${CMAKE_SOURCE_DIR}/src)

# Adding the libraries
add_subdirectory(libs)
