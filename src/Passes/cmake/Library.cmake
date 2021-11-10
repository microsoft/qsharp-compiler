macro(list_source_files result  directory)
  file(GLOB_RECURSE source RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${directory}/*.cpp)
  set(filelist "")
  foreach(child ${source})
    if(NOT ${child} MATCHES "(/Tests/)")
      list(APPEND filelist ${child})
    endif()
  endforeach()  

  set(${result} ${filelist})
endmacro()

function (microsoft_add_library          
          library)
  list(REMOVE_AT ARGV 0)

  set(directory ${CMAKE_CURRENT_SOURCE_DIR}/${library})

  list_source_files(source  ${directory})

  add_library(${library} 
              STATIC
              ${source})

  target_include_directories(
    ${library} 
    PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/include"
  )


  if (WIN32)
    # Windows specific congure
  elseif (APPLE)
    # OS X specific
    if(MICROSOFT_ENABLE_DYNAMIC_LOADING)
      target_link_libraries(${library} 
        PUBLIC "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")
    else()
      target_link_libraries(${library} PRIVATE ${llvm_libs})
    endif(MICROSOFT_ENABLE_DYNAMIC_LOADING)

  else () 
    # Assuming linux
    if(MICROSOFT_ENABLE_DYNAMIC_LOADING)
      target_compile_options(${library} 
        PUBLIC "-fPIC")
    else()
      target_link_libraries(${library} PRIVATE ${llvm_libs})
    endif(MICROSOFT_ENABLE_DYNAMIC_LOADING)    
  endif ()



endfunction ()
