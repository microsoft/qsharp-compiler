// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#ifndef SIMFACTORY_H
#define SIMFACTORY_H

#include <stdint.h>
#include "CoreDefines.h"

#ifdef __cplusplus
extern "C"
{
#endif

    QIR_SHARED_API void* CreateFullstateSimulatorC(uint32_t userProvidedSeed);

#ifdef __cplusplus
} // extern "C"
#endif

#endif // #ifndef SIMFACTORY_H
