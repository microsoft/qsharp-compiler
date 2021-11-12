if (MICROSOFT_ENABLE_TESTS)
  include(CTest)
  enable_testing()
endif (MICROSOFT_ENABLE_TESTS)


function (_internal_add_test name source library)
  if (MICROSOFT_ENABLE_TESTS)
    file(GLOB_RECURSE srcs RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${source}/*.cpp)
    file(GLOB_RECURSE hdrs RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${source}/*.hpp)
    file(GLOB_RECURSE ipps RELATIVE ${CMAKE_CURRENT_SOURCE_DIR} ${source}/*.ipp)

    add_executable(${name} ${ipps} ${hdrs} ${srcs})
    target_link_libraries(${name} PRIVATE ${llvm_libs})    
    target_link_libraries(${name} PRIVATE ${library} gmock gmock_main TestTools)  

    target_include_directories(${name} 
                                 PRIVATE ${MICROSOFT_ROOT_VENDOR_DIR}/googletest/googlemock/include)

    add_test(${name}
             ${name}
             --gtest_shuffle
             --gtest_random_seed=1337)  
  endif (MICROSOFT_ENABLE_TESTS)
endfunction()

function (microsoft_add_library_tests
          library)
  if (MICROSOFT_ENABLE_TESTS)
    list(REMOVE_AT ARGV 0)

    set(directory ${CMAKE_CURRENT_SOURCE_DIR}/${library}/Tests)    
    set(unit_directory ${directory}/Unit)    
    set(integration_directory ${directory}/Integration)    

     if(IS_DIRECTORY ${unit_directory})
       _internal_add_test("${library}UnitTests" ${unit_directory} ${library})
       target_link_libraries("${library}UnitTests" PRIVATE ${ARGV})
     else()
       message(NOTICE "No unit tests for ${library}")
     endif()

     if(IS_DIRECTORY ${integration_directory})
       _internal_add_test("${library}IntegrationTests" ${integration_directory} ${library})
       target_link_libraries("${library}IntegrationTests" PRIVATE ${ARGV})       
     else()
       message(NOTICE "No integration tests for ${library}")
     endif()
  endif (MICROSOFT_ENABLE_TESTS)
endfunction ()
