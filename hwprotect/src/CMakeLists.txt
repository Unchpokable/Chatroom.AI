set(HWPROTECT_SOURCES
    hwprotect_capi.cxx
)

set(HWPROTECT_HEADERS
    hwprotect_export.h
    # Add header files here
)

add_library(hwprotect SHARED
    ${HWPROTECT_SOURCES}
    ${HWPROTECT_HEADERS}
)

target_compile_definitions(hwprotect PRIVATE HWPROTECT_BUILD_SHARED)

target_include_directories(hwprotect
    PUBLIC
        $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}>
        $<INSTALL_INTERFACE:include>
)

# Link dependencies
target_link_libraries(hwprotect PRIVATE Identy)

# Set DLL properties
set_target_properties(hwprotect PROPERTIES
    VERSION ${PROJECT_VERSION}
    SOVERSION ${PROJECT_VERSION_MAJOR}
    WINDOWS_EXPORT_ALL_SYMBOLS OFF
)
