cmake_minimum_required(VERSION 3.4.3)

microsoft_add_library(TestTools)
microsoft_add_library_tests(TestTools)

microsoft_add_library(Commandline)

microsoft_add_library(AllocationManager)
microsoft_add_library_tests(AllocationManager)

microsoft_add_library(ProfilePass)
target_link_libraries(ProfilePass PRIVATE Rules)
# microsoft_add_library_tests(ProfilePass)


microsoft_add_library(Rules)
microsoft_add_library_tests(Rules Rules ProfilePass AllocationManager Commandline Profiles)

target_link_libraries(Rules PRIVATE AllocationManager)

microsoft_add_library(Profiles)
target_link_libraries(Profiles PRIVATE Rules)
microsoft_add_library_tests(Profiles Rules AllocationManager Commandline Profiles)

add_subdirectory(Apps)
