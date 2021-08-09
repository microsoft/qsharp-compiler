cmake_minimum_required(VERSION 3.4.3)

# Creating the rules library
file(GLOB sources RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_SOURCE_DIR}/Rules/*.cpp)

add_library(Rules 
            SHARED 
            ${sources})

target_include_directories(
  Rules
  PRIVATE
  "${CMAKE_CURRENT_SOURCE_DIR}/include"
)

target_link_libraries(Rules
  "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")


# Creating all of the passes library
add_subdirectory(Passes)

# Creating all commandline apps
add_subdirectory(Apps)