#ifndef HWPROTECT_EXPORT_H
#define HWPROTECT_EXPORT_H

#ifdef _WIN32
    #ifdef HWPROTECT_BUILD_SHARED
        #define HWPROTECT_EXPORT __declspec(dllexport)
    #else
        #define HWPROTECT_EXPORT __declspec(dllimport)
    #endif
#else
    #ifdef HWPROTECT_BUILD_SHARED
        #define HWPROTECT_EXPORT __attribute__((visibility("default")))
    #else
        #define HWPROTECT_EXPORT
    #endif
#endif

#endif // HWPROTECT_EXPORT_H
