cmake_minimum_required(VERSION 3.4.3)

microsoft_add_library(TestTools)

microsoft_add_library(Commandline)

microsoft_add_library(AllocationManager)
microsoft_add_library_tests(AllocationManager)

microsoft_add_library(Rules)
microsoft_add_library_tests(Rules)

target_link_libraries(Rules PRIVATE AllocationManager)

add_subdirectory(Passes)

microsoft_add_library(Profiles)
target_link_libraries(Profiles PRIVATE Rules ExpandStaticAllocation TransformationRule QirAllocationAnalysis)
microsoft_add_library_tests(Profiles ExpandStaticAllocation QirAllocationAnalysis TransformationRule Rules AllocationManager Commandline Profiles)

add_subdirectory(Apps)


add_executable(Xxx Profiles/Tests/Unit/main.cpp)

target_link_libraries(Xxx ${llvm_libs})
target_link_libraries(Xxx TestTools ExpandStaticAllocation QirAllocationAnalysis TransformationRule Rules AllocationManager Commandline Profiles)
target_include_directories(Xxx PRIVATE ${MICROSOFT_ROOT_VENDOR_DIR}/googletest/googlemock/include)
target_link_libraries(Xxx gmock gmock_main TestTools)   