cmake_minimum_required(VERSION 3.4.3)

project(QirPasses)
set(CMAKE_BUILD_TYPE "Debug")
option(MICROSOFT_ENABLE_TESTS               "Enable test targets"   ON)
option(MICROSOFT_ENABLE_DYNAMIC_LOADING     "Use dynamic loading"   ON)
option(MICROSOFT_GENERATE_COVERAGE          "Generate coverage"     OFF)

# Microsoft Settings
set(MICROSOFT_ROOT_PASSES_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(MICROSOFT_ROOT_VENDOR_DIR ${MICROSOFT_ROOT_PASSES_DIR}/vendors)
message(STATUS "Passes dir: ${MICROSOFT_ROOT_PASSES_DIR}")
message(STATUS "Vendor dir: ${MICROSOFT_ROOT_VENDOR_DIR}")

find_package(LLVM REQUIRED CONFIG)
include(CheckCXXCompilerFlag)

message(STATUS "Found LLVM ${LLVM_PACKAGE_VERSION}")
message(STATUS "Using LLVMConfig.cmake in: ${LLVM_DIR}")

set (CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} -fno-omit-frame-pointer -fsanitize=address,undefined -g -O0 -fno-sanitize-recover=all")
set (CMAKE_LINKER_FLAGS_DEBUG "${CMAKE_LINKER_FLAGS_DEBUG} -fno-omit-frame-pointer -fsanitize=address,undefined  -g -O0 -fno-sanitize-recover=all")
set (CMAKE_CXX_FLAGS_RELEASE "${CMAKE_CXX_FLAGS_RELEASE} -fno-omit-frame-pointer -fsanitize=address,undefined -g -O3 -fno-sanitize-recover=all")
set (CMAKE_LINKER_FLAGS_RELEASE "${CMAKE_LINKER_FLAGS_RELEASE} -fno-omit-frame-pointer -fsanitize=address,undefined  -g -O3 -fno-sanitize-recover=all")

# -fvisibility-inlines-hidden is set when building LLVM and on Darwin warnings
# are triggered if the passes library is built without this flag (though otherwise it
# builds fine). For consistency, add it here too.
check_cxx_compiler_flag("-fvisibility-inlines-hidden" SUPPORTS_FVISIBILITY_INLINES_HIDDEN_FLAG)
if(${SUPPORTS_FVISIBILITY_INLINES_HIDDEN_FLAG})
  if (${SUPPORTS_FVISIBILITY_INLINES_HIDDEN_FLAG} STREQUAL "1")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fvisibility-inlines-hidden")
  endif()
endif()


# Helper variable to determine if we are using clang compiler
set(using_clang_compiler FALSE)
if ( "${CMAKE_CXX_COMPILER_ID}" STREQUAL "AppleClang" OR "${CMAKE_CXX_COMPILER_ID}" STREQUAL "Clang")
  set(using_clang_compiler TRUE)
endif ()

# Add compiler flags if we are 
if (MICROSOFT_GENERATE_COVERAGE)
    message(STATUS "Generating coverage data")
    
    if (using_clang_compiler)
      # Generating coverage output
      set(CMAKE_CXX_FLAGS_DEBUG "-g -fprofile-instr-generate -fcoverage-mapping -O0")

      # Forcing debug build
      set(CMAKE_BUILD_TYPE "Debug")
    else ()
      message(SEND_ERROR "Coverage flag enabled but clang compiler not found, CMake will exit.")
    endif ()
endif (MICROSOFT_GENERATE_COVERAGE)



if(MICROSOFT_ENABLE_TESTS)
  add_subdirectory(${MICROSOFT_ROOT_VENDOR_DIR}/googletest)
  target_compile_options(gmock
                             INTERFACE
                             -Wno-everything)
  target_compile_options(gtest
                             INTERFACE
                             -Wno-everything)

endif(MICROSOFT_ENABLE_TESTS)


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
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Werror -Wno-documentation-unknown-command")

include(${MICROSOFT_ROOT_PASSES_DIR}/cmake/Testing.cmake)
include(${MICROSOFT_ROOT_PASSES_DIR}/cmake/Library.cmake)




# LLVM is normally built without RTTI. Be consistent with that.
if(NOT LLVM_ENABLE_RTTI)
  message(STATUS "RTTI is off")
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fno-rtti")
else()
  message(STATUS "RTTI is on")
endif()


# We export the compile commands which are needed by clang-tidy 
# to run the static analysis
set(CMAKE_EXPORT_COMPILE_COMMANDS ON)

# Adding LLVM include directories. We may choose
# to move this to a module level at a later point
include_directories(${LLVM_INCLUDE_DIRS})
link_directories(${LLVM_LIBRARY_DIRS})
add_definitions(${LLVM_DEFINITIONS})
include_directories(${CMAKE_SOURCE_DIR}/Source)
llvm_map_components_to_libnames(llvm_libs support core irreader passes orcjit x86asmparser x86codegen x86desc x86disassembler x86info interpreter objcarcopts)

message(STATUS "${llvm_libs}")


# Adding the libraries
add_subdirectory(Source)

add_subdirectory(ComponentExamples)
