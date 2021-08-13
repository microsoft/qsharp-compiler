// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <cstdint>

#ifdef _WIN32
#ifdef EXPORT_QIR_API
#define QIR_SHARED_API __declspec(dllexport)
#else
#define QIR_SHARED_API __declspec(dllimport)
#endif
#else
#define QIR_SHARED_API
#endif

// The core types will be exposed in the C-interfaces for interop, thus no
// namespaces or scoped enums can be used to define them.

/*==============================================================================
  Qubit & Result

  These two types are opaque to the clients: clients cannot directly create, delete,
  copy or check state of qubits and results. QUBIT* and RESULT* should never be
  dereferenced in client's code.
==============================================================================*/
class QUBIT;
typedef QUBIT* Qubit;

class RESULT;
typedef RESULT* Result;

enum ResultValue
{
    Result_Zero = 0,
    Result_One = 1,
    Result_Pending, // indicates that this is a deferred result
};

/*==============================================================================
  PauliId matrices
==============================================================================*/
enum PauliId : int32_t
{
    PauliId_I = 0,
    PauliId_X = 1,
    PauliId_Z = 2,
    PauliId_Y = 3,
};
