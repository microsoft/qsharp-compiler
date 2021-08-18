function (microsoft_add_library          
          library)
  list(REMOVE_AT ARGV 0)

  set(directory ${CMAKE_CURRENT_SOURCE_DIR}/${library})

  file(GLOB_RECURSE source RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${directory}/*.cpp)
  add_library(${library} 
              SHARED 
              ${source})

  target_include_directories(
    ${library} 
    PRIVATE
    "${CMAKE_CURRENT_SOURCE_DIR}/include"
  )

  if(MICROSOFT_ENABLE_DYNAMIC_LOADING)
    target_link_libraries(${library} 
      "$<$<PLATFORM_ID:Darwin>:-undefined dynamic_lookup>")
  else()
    target_link_libraries(${library}  ${llvm_libs})
  endif(MICROSOFT_ENABLE_DYNAMIC_LOADING)

endfunction ()