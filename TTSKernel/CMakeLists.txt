cmake_minimum_required(VERSION 3.20)

# vcpkg toolchain setup (MUST be before project())
if(DEFINED ENV{VCPKG_ROOT})
    set(CMAKE_TOOLCHAIN_FILE "$ENV{VCPKG_ROOT}/scripts/buildsystems/vcpkg.cmake"
        CACHE STRING "Vcpkg toolchain file")
endif()

project(TTSKernel LANGUAGES C CXX)

# Set C and C++ standards
set(CMAKE_C_STANDARD 17)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Output directories
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/lib)

# Sherpa-ONNX paths
set(SHERPA_ONNX_DIR ${CMAKE_SOURCE_DIR}/deps/Sherpa-ONNX)
set(SHERPA_ONNX_INCLUDE_DIR ${SHERPA_ONNX_DIR}/include)
set(SHERPA_ONNX_LIB_DIR ${SHERPA_ONNX_DIR}/lib)
set(SHERPA_ONNX_BIN_DIR ${SHERPA_ONNX_DIR}/bin)

set(JSON_INCLUDE_DIR ${CMAKE_SOURCE_DIR}/deps/json/include)
set(UULOG_INCLUDE_DIR ${CMAKE_SOURCE_DIR}/deps/uulog)

set(LIBSAMPLERATE_INCLUDE_DIR ${CMAKE_SOURCE_DIR}/deps/libsamplerate/include)
set(LIBSAMPLERATE_LIB_DIR ${CMAKE_SOURCE_DIR}/deps/libsamplerate/lib)

set(BSTHREADPOOL_INCLUDE_DIR ${CMAKE_SOURCE_DIR}/deps/BSThreadPool/include)

# find library and its headers
find_path(IXWEBSOCKET_INCLUDE_DIR ixwebsocket/IXWebSocket.h)
find_library(IXWEBSOCKET_LIBRARY ixwebsocket)

find_package(ixwebsocket CONFIG REQUIRED)

find_package(MbedTLS CONFIG REQUIRED)

add_subdirectory(deps/uulog)

# Create executable
add_executable(${PROJECT_NAME}
    src/main.cxx
    src/websocket.cxx
    src/pipechannel.cxx
    src/tts_onnx.cxx
    src/service.cxx
)

target_precompile_headers(${PROJECT_NAME} PRIVATE src/pch.hxx)

# Include directories
target_include_directories(${PROJECT_NAME} PRIVATE
    ${SHERPA_ONNX_INCLUDE_DIR}
    ${JSON_INCLUDE_DIR}
    ${UULOG_INCLUDE_DIR}
    ${IXWEBSOCKET_INCLUDE_DIR}
    ${LIBSAMPLERATE_INCLUDE_DIR}
    ${BSTHREADPOOL_INCLUDE_DIR}
)

# Link directories
target_link_directories(${PROJECT_NAME} PRIVATE
    ${SHERPA_ONNX_LIB_DIR}
    ${LIBSAMPLERATE_LIB_DIR}
)

# Link libraries
target_link_libraries(${PROJECT_NAME} PRIVATE
    sherpa-onnx-cxx-api
    sherpa-onnx-c-api
    ixwebsocket::ixwebsocket
    uulog
    MbedTLS::mbedtls
    samplerate
)

# Copy DLLs to output directory on Windows
if(WIN32)
    # Sherpa-ONNX DLLs - use BIN_DIR for DLLs, not LIB_DIR
    set(SHERPA_DLLS
        ${SHERPA_ONNX_BIN_DIR}/onnxruntime.dll
        ${SHERPA_ONNX_BIN_DIR}/onnxruntime_providers_shared.dll
        ${SHERPA_ONNX_BIN_DIR}/sherpa-onnx-c-api.dll
        ${SHERPA_ONNX_BIN_DIR}/sherpa-onnx-cxx-api.dll
        ${SHERPA_ONNX_BIN_DIR}/cargs.dll
    )

    # Try lib directory as fallback if bin doesn't have DLLs
    if(NOT EXISTS "${SHERPA_ONNX_BIN_DIR}/sherpa-onnx-c-api.dll")
        set(SHERPA_DLLS
            ${SHERPA_ONNX_LIB_DIR}/onnxruntime.dll
            ${SHERPA_ONNX_LIB_DIR}/onnxruntime_providers_shared.dll
            ${SHERPA_ONNX_LIB_DIR}/sherpa-onnx-c-api.dll
            ${SHERPA_ONNX_LIB_DIR}/sherpa-onnx-cxx-api.dll
            ${SHERPA_ONNX_LIB_DIR}/cargs.dll
        )
    endif()

    foreach(DLL ${SHERPA_DLLS})
        if(EXISTS "${DLL}")
            add_custom_command(TARGET ${PROJECT_NAME} POST_BUILD
                COMMAND ${CMAKE_COMMAND} -E copy_if_different
                "${DLL}"
                $<TARGET_FILE_DIR:${PROJECT_NAME}>
            )
        else()
            message(WARNING "Sherpa DLL not found: ${DLL}")
        endif()
    endforeach()

    # vcpkg DLLs are copied automatically by vcpkg toolchain (applocal.ps1)
    # Manual copying is not needed and may cause conflicts
endif()
