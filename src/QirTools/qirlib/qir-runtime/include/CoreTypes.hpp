// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <cstdint>
#include "CoreDefines.h"

// The core types will be exposed in the C-interfaces for interop, thus no
// namespaces or scoped enums can be used to define them.

/*==============================================================================
  Qubit & Result

  These two types are opaque to the clients: clients cannot directly create, delete,
  copy or check state of qubits and results. QUBIT* and RESULT* should never be
  dereferenced in client's code.
==============================================================================*/

// Although QIR uses an opaque pointer to the type "QUBIT", it never points to an actual memory
// and is never intended to be dereferenced anywhere - in the client code or in our runtime.
// Runtime always operates in terms of qubit ids, which are integers. Qubit ids are casted
// to this pointer type and stored as pointer values. This is done to ensure that qubit type
// is a unique type in the QIR.

class QUBIT;
typedef intptr_t QubitIdType;

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
