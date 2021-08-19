cmake_minimum_required(VERSION 3.4.3)

project(QirPasses)

option(MICROSOFT_ENABLE_TESTS               "Enable test targets"                   ON)
option(MICROSOFT_ENABLE_DYNAMIC_LOADING     "Use dynamic loading"                  ON)


# Microsoft Settings
set(MICROSOFT_ROOT_PASSES_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(MICROSOFT_ROOT_VENDOR_DIR ${MICROSOFT_ROOT_PASSES_DIR}/vendors)
message(STATUS "Passes dir: ${MICROSOFT_ROOT_PASSES_DIR}")
message(STATUS "Vendor dir: ${MICROSOFT_ROOT_VENDOR_DIR}")

find_package(LLVM REQUIRED CONFIG)
include(CheckCXXCompilerFlag)

message(STATUS "Found LLVM ${LLVM_PACKAGE_VERSION}")
message(STATUS "Using LLVMConfig.cmake in: ${LLVM_DIR}")


# -fvisibility-inlines-hidden is set when building LLVM and on Darwin warnings
# are triggered if llvm-tutor is built without this flag (though otherwise it
# builds fine). For consistency, add it here too.
check_cxx_compiler_flag("-fvisibility-inlines-hidden" SUPPORTS_FVISIBILITY_INLINES_HIDDEN_FLAG)
if(${SUPPORTS_FVISIBILITY_INLINES_HIDDEN_FLAG})
  if (${SUPPORTS_FVISIBILITY_INLINES_HIDDEN_FLAG} STREQUAL "1")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fvisibility-inlines-hidden")
  endif()
endif()



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

# llvm_map_components_to_libnames(llvm_libs all all-targets analysis asmparser asmprinter binaryformat bitreader bitstreamreader bitwriter cfguard codegen core coroutines coverage debuginfocodeview debuginfodwarf debuginfogsym debuginfomsf debuginfopdb demangle dlltooldriver dwarflinker engine executionengine extensions frontendopenmp fuzzmutate globalisel  instcombine instrumentation interpreter ipo irreader jitlink  libdriver lineeditor linker lto mc mca mcdisassembler mcjit mcparser  native nativecodegen  objcarcopts object objectyaml option orcerror orcjit passes profiledata remarks runtimedyld scalaropts selectiondag support symbolize tablegen target textapi transformutils vectorize  windowsmanifest x86asmparser x86codegen x86desc x86disassembler x86info xray)
# llvm_map_components_to_libnames(llvm_libs all all-targets analysis asmparser asmprinter binaryformat bitreader bitstreamreader bitwriter cfguard codegen core coroutines coverage debuginfocodeview debuginfodwarf debuginfogsym debuginfomsf debuginfopdb demangle dlltooldriver dwarflinker engine executionengine extensions frontendopenmp fuzzmutate globalisel  instcombine instrumentation  ipo irreader jitlink  libdriver lineeditor linker lto mc mca mcdisassembler mcjit mcparser  native nativecodegen  objcarcopts object objectyaml option orcerror orcjit passes profiledata remarks runtimedyld scalaropts selectiondag support symbolize tablegen target textapi transformutils vectorize  windowsmanifest x86asmparser x86codegen x86desc x86disassembler x86info xray)
set(llvm_libs "LLVMAnalysis;LLVMAsmParser;LLVMAsmPrinter;LLVMBinaryFormat;LLVMBitReader;LLVMBitstreamReader;LLVMBitWriter;LLVMCFGuard;LLVMCodeGen;LLVMCore;LLVMCoroutines;LLVMCoverage;LLVMDebugInfoCodeView;LLVMDebugInfoDWARF;LLVMDebugInfoGSYM;LLVMDebugInfoMSF;LLVMDebugInfoPDB;LLVMDemangle;LLVMDlltoolDriver;LLVMDWARFLinker;LLVMExecutionEngine;LLVMExtensions;LLVMFrontendOpenMP;LLVMFuzzMutate;LLVMGlobalISel;LLVMInstCombine;LLVMInstrumentation;LLVMipo;LLVMIRReader;LLVMJITLink;LLVMLibDriver;LLVMLineEditor;LLVMLinker;LLVMLTO;LLVMMC;LLVMMCA;LLVMMCDisassembler;LLVMMCJIT;LLVMMCParser;LLVMX86CodeGen;LLVMX86Desc;LLVMX86Info;LLVMObjCARCOpts;LLVMObject;LLVMObjectYAML;LLVMOption;LLVMOrcError;LLVMOrcJIT;LLVMPasses;LLVMProfileData;LLVMRemarks;LLVMRuntimeDyld;LLVMScalarOpts;LLVMSelectionDAG;LLVMSupport;LLVMSymbolize;LLVMTableGen;LLVMTarget;LLVMTextAPI;LLVMTransformUtils;LLVMVectorize;LLVMWindowsManifest;LLVMX86AsmParser;LLVMX86CodeGen;LLVMX86Desc;LLVMX86Disassembler;LLVMX86Info;LLVMXRay;LLVMX86CodeGen;LLVMX86AsmParser;LLVMX86Desc;LLVMX86Disassembler;LLVMX86Info")
message(STATUS "${llvm_libs}")


# Adding the libraries
add_subdirectory(Source)
add_subdirectory(tests)

