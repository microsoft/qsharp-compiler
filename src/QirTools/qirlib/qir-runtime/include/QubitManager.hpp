// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma once

#include <iostream>
#include <vector>
#include <limits>

#include "CoreTypes.hpp"

namespace Microsoft
{
namespace Quantum
{
    // CQubitManager maintains mapping between user qubit objects and
    // underlying qubit identifiers (Ids). When user program allocates
    // a qubit, Qubit Manager decides whether to allocate a fresh id or
    // reuse existing id that was previously freed. When user program
    // releases a qubit, Qubit Manager tracks it as a free qubit id.
    // Decision to reuse a qubit id is influenced by restricted reuse
    // areas. When a qubit id is freed in one section of a restricted
    // reuse area, it cannot be reused in other sections of the same area.
    // True borrowing of qubits is not supported and is currently
    // implemented as a plain allocation.
    class QIR_SHARED_API CQubitManager
    {
      public:
        // We want status array to be reasonably large.
        constexpr static QubitIdType DefaultQubitCapacity = 8;

        // Indexes in the status array can potentially be in range 0 .. QubitId.MaxValue-1.
        // This gives maximum capacity as QubitId.MaxValue. Actual configured capacity may be less than this.
        // Index equal to QubitId.MaxValue doesn't exist and is reserved for 'NoneMarker' - list terminator.
        constexpr static QubitIdType MaximumQubitCapacity = std::numeric_limits<QubitIdType>::max();

      public:
        CQubitManager(QubitIdType initialQubitCapacity = DefaultQubitCapacity, bool mayExtendCapacity = true);

        // No complex scenarios for now. Don't need to support copying/moving.
        CQubitManager(const CQubitManager&) = delete;
        CQubitManager& operator=(const CQubitManager&) = delete;
        virtual ~CQubitManager();

        // Restricted reuse area control
        void StartRestrictedReuseArea();
        void NextRestrictedReuseSegment();
        void EndRestrictedReuseArea();

        // Allocate a qubit. Extend capacity if necessary and possible.
        // Fail if the qubit cannot be allocated.
        // Computation complexity is O(number of nested restricted reuse areas) worst case, O(1) amortized cost.
        QubitIdType Allocate();
        // Allocate qubitCountToAllocate qubits and store them in the provided array.
        // Extend manager capacity if necessary and possible.
        // Fail without allocating any qubits if the qubits cannot be allocated.
        // Caller is responsible for providing array of sufficient size to hold qubitCountToAllocate.
        void Allocate(QubitIdType* qubitsToAllocate, int32_t qubitCountToAllocate);

        // Releases a given qubit.
        void Release(QubitIdType qubit);
        // Releases qubitCountToRelease qubits in the provided array.
        // Caller is responsible for managing memory used by the array itself
        // (i.e. delete[] array if it was dynamically allocated).
        void Release(QubitIdType* qubitsToRelease, int32_t qubitCountToRelease);

        // Borrow (We treat borrowing as allocation currently)
        QubitIdType Borrow();
        void Borrow(QubitIdType* qubitsToBorrow, int32_t qubitCountToBorrow);
        // Return (We treat returning as release currently)
        void Return(QubitIdType qubit);
        void Return(QubitIdType* qubitsToReturn, int32_t qubitCountToReturn);

        // Disables a given qubit.
        // Once a qubit is disabled it can never be "enabled" or reallocated.
        void Disable(QubitIdType qubit);
        // Disables a set of given qubits.
        // Once a qubit is disabled it can never be "enabled" or reallocated.
        void Disable(QubitIdType* qubitsToDisable, int32_t qubitCountToDisable);

        bool IsValidQubit(QubitIdType qubit) const;
        bool IsDisabledQubit(QubitIdType qubit) const;
        bool IsExplicitlyAllocatedQubit(QubitIdType qubit) const;
        bool IsFreeQubitId(QubitIdType id) const;

        // Qubit counts:

        // Number of qubits that are disabled. When an explicitly allocated qubit
        // gets disabled, it is removed from allocated count and is added to
        // disabled count immediately. Subsequent Release doesn't affect counts.
        QubitIdType GetDisabledQubitCount() const
        {
            return disabledQubitCount;
        }

        // Number of qubits that are explicitly allocated. This counter gets
        // increased on allocation of a qubit and decreased on release of a qubit.
        // Note that we treat borrowing as allocation now.
        QubitIdType GetAllocatedQubitCount() const
        {
            return allocatedQubitCount;
        }

        // Number of free qubits that are currently tracked by this qubit manager.
        // Note that when qubit manager may extend capacity, this doesn't account
        // for qubits that may be potentially added in future via capacity extension.
        // If qubit manager may extend capacity and reuse is discouraged, released
        // qubits still increase this number even though they cannot be reused.
        QubitIdType GetFreeQubitCount() const
        {
            return freeQubitCount;
        }

        // Total number of qubits that are currently tracked by this qubit manager.
        QubitIdType GetQubitCapacity() const
        {
            return qubitCapacity;
        }
        bool GetMayExtendCapacity() const
        {
            return mayExtendCapacity;
        }

      private:
        // The end of free lists are marked with NoneMarker value. It is used like null for pointers.
        // This value is non-negative just like other values in the free lists. See sharedQubitStatusArray.
        constexpr static QubitIdType NoneMarker = std::numeric_limits<QubitIdType>::max();

        // Explicitly allocated qubits are marked with AllocatedMarker value.
        // If borrowing is implemented, negative values may be used for refcounting.
        // See sharedQubitStatusArray.
        constexpr static QubitIdType AllocatedMarker = std::numeric_limits<QubitIdType>::min();

        // Disabled qubits are marked with this value. See sharedQubitStatusArray.
        constexpr static QubitIdType DisabledMarker = -1;

        // QubitListInSharedArray implements a singly-linked list with "pointers"
        // to the first and the last element stored. Pointers are the indexes
        // in a single shared array. Shared array isn't sotored in this class
        // because it can be reallocated. This class maintains status of elements
        // in the list by virtue of linking them as part of this list. This class
        // sets Allocated status of elementes taken from the list (via TakeQubitFromFront).
        // This class is small, contains no C++ pointers and relies on default shallow copying/destruction.
        struct QubitListInSharedArray final
        {
          private:
            QubitIdType firstElement = NoneMarker;
            QubitIdType lastElement  = NoneMarker;
            // We are not storing pointer to shared array because it can be reallocated.
            // Indexes and special values remain the same on such reallocations.

          public:
            // Initialize empty list
            QubitListInSharedArray() = default;

            // Initialize as a list with sequential elements from startId to endId inclusve.
            QubitListInSharedArray(QubitIdType startId, QubitIdType endId, QubitIdType* sharedQubitStatusArray);

            bool IsEmpty() const;
            void AddQubit(QubitIdType id, QubitIdType* sharedQubitStatusArray);
            QubitIdType TakeQubitFromFront(QubitIdType* sharedQubitStatusArray);
            void MoveAllQubitsFrom(QubitListInSharedArray& source, QubitIdType* sharedQubitStatusArray);
        };

        // Restricted reuse area consists of multiple segments. Qubits released
        // in one segment cannot be reused in another. One restricted reuse area
        // can be nested in a segment of another restricted reuse area. This class
        // tracks current segment of an area by maintaining a list of free qubits
        // in a shared status array FreeQubitsReuseAllowed. Previous segments are
        // tracked collectively (not individually) by maintaining FreeQubitsReuseProhibited.
        // This class is small, contains no C++ pointers and relies on default shallow copying/destruction.
        struct RestrictedReuseArea final
        {
          public:
            QubitListInSharedArray FreeQubitsReuseProhibited;
            QubitListInSharedArray FreeQubitsReuseAllowed;
            // When we are looking for free qubits we skip areas that are known not
            // to have them (to achieve amortized cost O(1)). It is guaranteed that
            // there're no free qubits available in areas between this one and the
            // one pointed to by prevAreaWithFreeQubits (bounds non-inclusinve).
            // This invariant is maintained in all operations. It is NOT guaranteed
            // that the area pointed to by prevAreaWithFreeQubits actually has
            // free qubits available, and the search may need to continue.
            int32_t prevAreaWithFreeQubits = 0;

            RestrictedReuseArea() = default;
            explicit RestrictedReuseArea(QubitListInSharedArray freeQubits);
        };

        // This is NOT a pure stack! We modify it only by push/pop, but we also iterate over elements.
        class CRestrictedReuseAreaStack final : public std::vector<RestrictedReuseArea>
        {
          public:
            // No complex scenarios for now. Don't need to support copying/moving.
            CRestrictedReuseAreaStack()                                 = default;
            CRestrictedReuseAreaStack(const CRestrictedReuseAreaStack&) = delete;
            CRestrictedReuseAreaStack& operator=(const CRestrictedReuseAreaStack&) = delete;
            CRestrictedReuseAreaStack(CRestrictedReuseAreaStack&&)                 = delete;
            CRestrictedReuseAreaStack& operator=(CRestrictedReuseAreaStack&&) = delete;
            ~CRestrictedReuseAreaStack()                                      = default;

            void PushToBack(RestrictedReuseArea area);
            RestrictedReuseArea PopFromBack();
            RestrictedReuseArea& PeekBack();
            [[nodiscard]] int32_t Count() const;
        };

        void EnsureCapacity(QubitIdType requestedCapacity);

        // Take free qubit id from a free list without extending capacity.
        // Free list to take from is found by amortized O(1) algorithm.
        QubitIdType TakeFreeQubitId();
        // Allocate free qubit id extending capacity if necessary and possible.
        QubitIdType AllocateQubitId();
        // Put qubit id back into a free list for the current restricted reuse area.
        void ReleaseQubitId(QubitIdType id);

        [[nodiscard]] bool IsValidId(QubitIdType id) const;
        [[nodiscard]] bool IsDisabledId(QubitIdType id) const;
        [[nodiscard]] bool IsFreeId(QubitIdType id) const;
        [[nodiscard]] bool IsExplicitlyAllocatedId(QubitIdType id) const;

      private:
        // Configuration Properties:
        bool mayExtendCapacity = true;

        // State:
        // sharedQubitStatusArray is used to store statuses of all known qubits.
        // Integer value at the index of the qubit id represents the status of that qubit.
        // (Ex: sharedQubitStatusArray[4] is the status of qubit with id = 4).
        // Therefore qubit ids are in the range of [0..qubitCapacity).
        // Capacity may be extended if MayExtendCapacity = true.
        // If qubit X is allocated, sharedQubitStatusArray[X] = AllocatedMarker (negative number)
        // If qubit X is disabled, sharedQubitStatusArray[X] = DisabledMarker (negative number)
        // If qubit X is free, sharedQubitStatusArray[X] is a non-negative number, denote it Next(X).
        // Next(X) is either the index of the next element in the list or the list terminator - NoneMarker.
        // All free qubits form disjoint singly linked lists bound to to respective resricted reuse areas.
        // Each area has two lists of free qubits - see RestrictedReuseArea.
        QubitIdType* sharedQubitStatusArray = nullptr;
        // qubitCapacity is always equal to the array size.
        QubitIdType qubitCapacity = 0;
        // All nested restricted reuse areas at the current moment.
        // Fresh Free Qubits are added to the outermost area: freeQubitsInAreas[0].FreeQubitsReuseAllowed
        CRestrictedReuseAreaStack freeQubitsInAreas;

        // Counts:
        QubitIdType disabledQubitCount  = 0;
        QubitIdType allocatedQubitCount = 0;
        QubitIdType freeQubitCount      = 0;
    };

} // namespace Quantum
} // namespace Microsoft
