add_executable(qat Qat/Qat.cpp)

target_link_libraries(qat ${llvm_libs})
target_link_libraries(qat ExpandStaticAllocation QubitAllocationAnalysis TransformationRule Rules AllocationManager Commandline Profiles)