add_library(HelloWorld SHARED HelloWorld/main.cpp)

if (WIN32)
  # Windows specific configure
  # TODO(tfr): Add Windows specific configuration
elseif (APPLE)
  target_link_libraries(HelloWorld
    PUBLIC "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")
else () 
  target_compile_options(HelloWorld
    PUBLIC "-fPIC")
endif ()


add_library(InlinePassComponent SHARED InlinePassComponent/main.cpp)

if (WIN32)
  # Windows specific configure
  # TODO(tfr): Add Windows specific configuration  
elseif (APPLE)
  target_link_libraries(InlinePassComponent
    PUBLIC "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")
else () 
  target_compile_options(InlinePassComponent
    PUBLIC "-fPIC")
endif ()



add_library(CArrayMap SHARED CArrayMap/main.cpp)

if (WIN32)
  # Windows specific configure
  # TODO(tfr): Add Windows specific configuration  
elseif (APPLE)
  target_link_libraries(CArrayMap
    PUBLIC "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")
else () 
  target_compile_options(CArrayMap
    PUBLIC "-fPIC")
endif ()