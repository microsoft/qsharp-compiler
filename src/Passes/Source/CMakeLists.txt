cmake_minimum_required(VERSION 3.4.3)

# Creating Allocation Manager library
file(GLOB COMMANDLINE_SRC RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/Commandline/*.cpp)
add_library(Commandline 
            SHARED 
            ${COMMANDLINE_SRC})

target_include_directories(
  Commandline
  PRIVATE
  "${CMAKE_CURRENT_SOURCE_DIR}/include"
)

target_link_libraries(Commandline
  "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")


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
file(GLOB RULES_OPRANDS_SOURCE RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/Rules/Operands/*.cpp)
file(GLOB RULES_NOTATION_SOURCE RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/Rules/Notation/*.cpp)

add_library(Rules 
            SHARED 
            ${RULES_SOURCE}
            ${RULES_OPRANDS_SOURCE}
            ${RULES_NOTATION_SOURCE} )

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




# Creating Allocation Manager library
file(GLOB PROFILE_SOURCE RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/Profiles/*.cpp)
add_library(Profiles 
            SHARED 
            ${PROFILE_SOURCE})

target_include_directories(
  Profiles
  PRIVATE
  "${CMAKE_CURRENT_SOURCE_DIR}/include"
)

target_link_libraries(Profiles
  "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")



# Creating all commandline apps
add_subdirectory(Apps)