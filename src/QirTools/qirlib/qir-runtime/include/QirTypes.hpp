// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <cstddef>
#include <string>
#include <vector>
#include <cstdint>

#include "CoreTypes.hpp"

/*======================================================================================================================
    QirArray
======================================================================================================================*/
struct QIR_SHARED_API QirArray
{
    using TItemCount = uint32_t;   // Data type of number of items (potentially can be increased to `uint64_t`).
    using TItemSize  = uint32_t;   // Data type of item size.
    using TBufSize   = size_t;     // Size of the buffer pointed to by `buffer`
                                   // (32 bit on 32-bit arch, 64 bit on 64-bit arch).
    using TDimCount     = uint8_t; // Data type for number of dimensions (3 for 3D array).
    using TDimContainer = std::vector<TItemCount>; // Data type for container of dimensions
                                                   // (for array 2x3x5 3 items: 2, 3, 5).

    // The product of all dimensions (2x3x5 = 30) should be equal to the overall number of items - `count`,
    // i.e. the product must fit in `TItemCount`. That is why `TItemCount` should be sufficient to store each dimension.

    TItemCount count                = 0; // Overall number of elements in the array across all dimensions
    const TItemSize itemSizeInBytes = 0;

    const TDimCount dimensions = 1;
    TDimContainer dimensionSizes; // not set for 1D arrays, as `count` is sufficient

    char* buffer = nullptr;

    bool ownsQubits = false;
    int refCount    = 1;
    int aliasCount  = 0; // used to enable copy elision, see the QIR specifications for details

    // NB: Release doesn't trigger destruction of the Array itself (only of its data buffer) to allow for it being used
    // both on the stack and on the heap. The creator of the array should delete it, if allocated from the heap.
    int AddRef();
    int Release();

    explicit QirArray(TItemCount cQubits);
    QirArray(TItemCount cItems, TItemSize itemSizeInBytes, TDimCount dimCount = 1, TDimContainer&& dimSizes = {});
    QirArray(const QirArray& other);

    ~QirArray();

    [[nodiscard]] char* GetItemPointer(TItemCount index) const;
    void Append(const QirArray* other);
};

/*======================================================================================================================
    QirString is just a wrapper around std::string
======================================================================================================================*/
struct QIR_SHARED_API QirString
{
    long refCount = 1;
    std::string str;

    explicit QirString(std::string&& str);
    explicit QirString(const char* cstr);
};

/*======================================================================================================================
    Tuples are opaque to the runtime and the type of the data contained in them isn't (generally) known, thus, we use
    char* to represent the tuples QIR operates with. However, we need to manage tuples' lifetime and in case of nested
    controlled callables we also need to peek into the tuple's content. To do this we associate with each tuple's buffer
    a header that contains the relevant data. The header immediately precedes the tuple's buffer in memory when the
    tuple is created.
======================================================================================================================*/
// TODO (rokuzmin): Move these types to inside of `QirTupleHeader`.
using PTuplePointedType = uint8_t;
using PTuple = PTuplePointedType*; // TODO(rokuzmin): consider replacing `uint8_t*` with `void*` in order to block
                                   //       the accidental {dereferencing and pointer arithmetic}.
                                   //       Much pointer arithmetic in tests. GetHeader() uses the pointer arithmetic.
struct QIR_SHARED_API QirTupleHeader
{
    using TBufSize = size_t; // Type of the buffer size.

    int refCount       = 0;
    int32_t aliasCount = 0; // used to enable copy elision, see the QIR specifications for details
    TBufSize tupleSize = 0; // when creating the tuple, must be set to the size of the tuple's data buffer (in bytes)

    // flexible array member, must be last in the struct
    PTuplePointedType data[];

    PTuple AsTuple()
    {
        return (PTuple)data;
    }

    int AddRef();
    int Release();

    static QirTupleHeader* Create(TBufSize size);
    static QirTupleHeader* CreateWithCopiedData(QirTupleHeader* other);

    static QirTupleHeader* GetHeader(PTuple tuple)
    {
        return reinterpret_cast<QirTupleHeader*>(tuple - offsetof(QirTupleHeader, data));
    }
};

/*======================================================================================================================
    A helper type for unpacking tuples used by multi-level controlled callables
======================================================================================================================*/
struct QIR_SHARED_API TupleWithControls
{
    QirArray* controls;
    TupleWithControls* innerTuple;

    PTuple AsTuple()
    {
        return reinterpret_cast<PTuple>(this);
    }

    static TupleWithControls* FromTuple(PTuple tuple)
    {
        return reinterpret_cast<TupleWithControls*>(tuple);
    }

    static TupleWithControls* FromTupleHeader(QirTupleHeader* th)
    {
        return FromTuple(th->AsTuple());
    }

    QirTupleHeader* GetHeader()
    {
        return QirTupleHeader::GetHeader(this->AsTuple());
    }
};
static_assert(sizeof(TupleWithControls) == 2 * sizeof(void*),
              L"TupleWithControls must be tightly packed for FlattenControlArrays to be correct");

/*======================================================================================================================
    QirCallable
======================================================================================================================*/
typedef void (*t_CallableEntry)(PTuple, PTuple, PTuple); // TODO(rokuzmin): Move to `QirCallable::t_CallableEntry`.
typedef void (*t_CaptureCallback)(PTuple, int32_t);      // TODO(rokuzmin): Move to `QirCallable::t_CaptureCallback`.
struct QIR_SHARED_API QirCallable
{
    static int constexpr Adjoint    = 1;
    static int constexpr Controlled = 1u << 1;

  private:
    static int constexpr TableSize = 4;
    static_assert(QirCallable::Adjoint + QirCallable::Controlled < QirCallable::TableSize,
                  L"functor kind is used as index into the functionTable");

    int refCount   = 1;
    int aliasCount = 0;

    // If the callable doesn't support Adjoint or Controlled functors, the corresponding entries in the table should be
    // set to nullptr.
    t_CallableEntry functionTable[QirCallable::TableSize] = {nullptr, nullptr, nullptr, nullptr};

    static int constexpr CaptureCallbacksTableSize                             = 2;
    t_CaptureCallback captureCallbacks[QirCallable::CaptureCallbacksTableSize] = {nullptr, nullptr};

    // The callable stores the capture, it's given at creation, and passes it to the functions from the function table,
    // but the runtime doesn't have any knowledge about what the tuple actually is.
    PTuple const capture = nullptr;

    // By default the callable is neither adjoint nor controlled.
    int appliedFunctor = 0;

    // Per https://github.com/microsoft/qsharp-language/blob/main/Specifications/QIR/Callables.md, the callable must
    // unpack the nested controls from the input tuples. Because the tuples aren't typed, the callable will assume
    // that its input tuples are formed in a particular way and will extract the controls to match its tracked depth.
    int controlledDepth = 0;

    // Prevent stack allocations.
    ~QirCallable();

  public:
    QirCallable(const t_CallableEntry* ftEntries, const t_CaptureCallback* captureCallbacks, PTuple capture);
    QirCallable(const QirCallable& other);
    QirCallable* CloneIfShared();

    int AddRef();
    int Release();
    void UpdateAliasCount(int increment);

    void Invoke(PTuple args, PTuple result);
    void Invoke(); // a shortcut to invoke a callable with no arguments and Unit result
    void ApplyFunctor(int functor);

    void InvokeCaptureCallback(int32_t index, int32_t parameter);
};

extern "C"
{
    // https://docs.microsoft.com/azure/quantum/user-guide/language/expressions/valueliterals#range-literals
    struct QirRange
    {
        int64_t start; // Inclusive.
        int64_t step;
        int64_t end; // Inclusive.
    };
}
