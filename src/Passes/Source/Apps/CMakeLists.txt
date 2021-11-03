add_executable(qat Qat/Qat.cpp Qat/QatConfig.cpp)

target_link_libraries(qat ${llvm_libs})
target_link_libraries(qat RuleTransformationPass Rules AllocationManager Commandline Generators Profile Validator ValidationPass)
