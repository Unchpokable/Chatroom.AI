#pragma once

#ifndef HWPROTECT_CAPI_H
#define HWPROTECT_CAPI_H

#include "hwprotect_export.h"

#include <stdint.h>

struct HWID_SHA256
{
    unsigned char data[32];
};

#ifdef __cplusplus
extern "C" {
#endif

HWPROTECT_EXPORT void hwprotect_encrypt_string(const char* source_str, char* buffer, int32_t length, bool snap_drives);
HWPROTECT_EXPORT void hwprotect_decrypt_string(const char* encrypted_str, int32_t encrypted_str_length, char* out_buffer,
    int32_t out_buffer_length, bool snap_drives);

HWPROTECT_EXPORT HWID_SHA256 hwprotect_get_hwid(bool snap_drives);

#ifdef __cplusplus
}
#endif

#endif