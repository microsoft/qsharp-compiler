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

// Although "Qubit" type is declared as a pointer to "QUBIT", it never points to an actual memory
// and is never intended to be dereferenced anywhere - in the client code or in the runtime.
// Runtime always operates in terms of qubit ids, which are integers. Qubit ids are casted
// to this pointer type and stored as pointer values. This is done to ensure that qubit type
// is a unique type in the QIR.

class QUBIT;
typedef QUBIT* Qubit; // Not a pointer to a memory location, just an integer - qubit id.

class RESULT;
typedef RESULT* Result; // TODO(rokuzmin): Replace with `typedef uintXX_t Result`, where XX is 8|16|32|64.
                        //       Remove all the `RESULT`.

enum ResultValue
{
    Result_Zero = 0,
    Result_One  = 1,
    Result_Pending, // indicates that this is a deferred result
};

/*==============================================================================
  PauliId matrices
==============================================================================*/
enum PauliId : int8_t
{
    PauliId_I = 0,
    PauliId_X = 1,
    PauliId_Z = 2,
    PauliId_Y = 3,
};
