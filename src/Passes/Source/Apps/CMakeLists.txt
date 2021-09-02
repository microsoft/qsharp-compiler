add_executable(qat Qat/Qat.cpp Qat/LlvmAnalysis.cpp)

target_link_libraries(qat ${llvm_libs})
target_link_libraries(qat Profile Rules AllocationManager Commandline Profiles)