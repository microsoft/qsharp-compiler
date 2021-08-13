// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <cstddef>
#include <string>
#include <vector>

#include "CoreTypes.hpp"

/*======================================================================================================================
    QirArray
======================================================================================================================*/
struct QIR_SHARED_API QirArray
{
    int64_t count = 0; // Overall number of elements in the array across all dimensions
    const int itemSizeInBytes = 0;

    const int dimensions = 1;
    std::vector<int64_t> dimensionSizes; // not set for 1D arrays, as `count` is sufficient

    char* buffer = nullptr;

    bool ownsQubits = false;
    int refCount = 1;
    int aliasCount = 0; // used to enable copy elision, see the QIR specifications for details

    // NB: Release doesn't trigger destruction of the Array itself (only of its data buffer) to allow for it being used
    // both on the stack and on the heap. The creator of the array should delete it, if allocated from the heap.
    int AddRef();
    int Release();

    QirArray(int64_t cQubits);
    QirArray(int64_t cItems, int itemSizeInBytes, int dimCount = 1, std::vector<int64_t>&& dimSizes = {});
    QirArray(const QirArray* other);

    ~QirArray();

    char* GetItemPointer(int64_t index);
    void Append(const QirArray* other);
};

/*======================================================================================================================
    QirString is just a wrapper around std::string
======================================================================================================================*/
struct QIR_SHARED_API QirString
{
    long refCount = 1;
    std::string str;

    QirString(std::string&& str);
    QirString(const char* cstr);
};

/*======================================================================================================================
    Tuples are opaque to the runtime and the type of the data contained in them isn't (generally) known, thus, we use
    char* to represent the tuples QIR operates with. However, we need to manage tuples' lifetime and in case of nested
    controlled callables we also need to peek into the tuple's content. To do this we associate with each tuple's buffer
    a header that contains the relevant data. The header immediately precedes the tuple's buffer in memory when the
    tuple is created.
======================================================================================================================*/
using PTuple = char*;   // TODO: consider replacing `char*` with `void*` in order to block the accidental {dereferencing and pointer arithmtic}.
struct QIR_SHARED_API QirTupleHeader
{
    int     refCount = 0;
    int32_t aliasCount = 0; // used to enable copy elision, see the QIR specifications for details
    int32_t tupleSize = 0; // when creating the tuple, must be set to the size of the tuple's data buffer (in bytes)

    // flexible array member, must be last in the struct
    char data[];

    PTuple AsTuple()
    {
        return data;
    }

    int AddRef();
    int Release();

    static QirTupleHeader* Create(int size);
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
static_assert(
    sizeof(TupleWithControls) == 2 * sizeof(void*),
    L"TupleWithControls must be tightly packed for FlattenControlArrays to be correct");

/*======================================================================================================================
    QirCallable
======================================================================================================================*/
typedef void (*t_CallableEntry)(PTuple, PTuple, PTuple);    // TODO: Move to `QirCallable::t_CallableEntry`.
typedef void (*t_CaptureCallback)(PTuple, int32_t);         // TODO: Move to `QirCallable::t_CaptureCallback`.
struct QIR_SHARED_API QirCallable
{
    static int constexpr Adjoint = 1;
    static int constexpr Controlled = 1 << 1;

  private:
    static int constexpr TableSize = 4;
    static_assert(
        QirCallable::Adjoint + QirCallable::Controlled < QirCallable::TableSize,
        L"functor kind is used as index into the functionTable");

    int refCount = 1;
    int aliasCount = 0;

    // If the callable doesn't support Adjoint or Controlled functors, the corresponding entries in the table should be
    // set to nullptr.
    t_CallableEntry functionTable[QirCallable::TableSize] = {nullptr, nullptr, nullptr, nullptr};

    static int constexpr CaptureCallbacksTableSize = 2;
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

struct QIR_SHARED_API QirRange
{
    int64_t start;
    int64_t step;
    int64_t end;

    QirRange();
    QirRange(int64_t start, int64_t step, int64_t end);
};
