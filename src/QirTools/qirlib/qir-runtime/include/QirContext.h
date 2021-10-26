#ifndef QIRCONTEXT_H
#define QIRCONTEXT_H

#include <stdbool.h>
#include "CoreDefines.h"

#ifdef __cplusplus
extern "C"
{
#endif

    QIR_SHARED_API void InitializeQirContext(void* driver, bool trackAllocatedObjects);

#ifdef __cplusplus
} // extern "C"
#endif

#endif // #ifndef QIRCONTEXT_H
