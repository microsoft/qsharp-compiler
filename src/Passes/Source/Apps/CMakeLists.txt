add_executable(qat Qat/Qat.cpp Qat/LlvmAnalysis.cpp Qat/Config.cpp)

target_link_libraries(qat ${llvm_libs})
target_link_libraries(qat RuleTransformationPass Rules AllocationManager Commandline Generators)