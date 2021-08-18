cmake_minimum_required(VERSION 3.4.3)

microsoft_add_library(Commandline)

microsoft_add_library(AllocationManager)

microsoft_add_library(Rules)
target_link_libraries(Rules AllocationManager)

add_subdirectory(Passes)

microsoft_add_library(Profiles)
target_link_libraries(Profiles Rules ExpandStaticAllocation TransformationRule QirAllocationAnalysis)

add_subdirectory(Apps)