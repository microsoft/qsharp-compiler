cmake_minimum_required(VERSION 3.4.3)

# Creating Allocation Manager library
file(GLOB ALLOCATION_MGR_SOURCE RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/AllocationManager/*.cpp)
add_library(AllocationManager 
            SHARED 
            ${ALLOCATION_MGR_SOURCE})

target_include_directories(
  AllocationManager
  PRIVATE
  "${CMAKE_CURRENT_SOURCE_DIR}/include"
)

target_link_libraries(AllocationManager
  "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")

# Creating the rules library
file(GLOB RULES_SOURCE RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/Rules/*.cpp)

add_library(Rules 
            SHARED 
            ${RULES_SOURCE} )

target_include_directories(
  Rules
  PRIVATE
  "${CMAKE_CURRENT_SOURCE_DIR}/include"
)

target_link_libraries(Rules AllocationManager)
target_link_libraries(Rules
  "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")


# Creating all of the passes library
add_subdirectory(Passes)

# Creating all commandline apps
add_subdirectory(Apps)