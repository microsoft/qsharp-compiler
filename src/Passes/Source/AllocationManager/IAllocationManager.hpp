#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "QatTypes/QatTypes.hpp"

#include <memory>
#include <string>

namespace microsoft
{
namespace quantum
{
    /// Interface class for allocation management. This interface provides means to allocate and release
    /// statically allocated resources such as qubits and results. In a future version, it may be
    /// extended with get and store in order to support Arrays and Tuples.
    class IAllocationManager
    {
      public:
        using Address              = uint64_t; ///< Value type for address
        using Index                = uint64_t; ///< Index type used to access an array element.
        using AllocationManagerPtr = std::shared_ptr<IAllocationManager>; ///< Pointer interface.

        // Construction, moves and copies
        //
        IAllocationManager(IAllocationManager const&) = delete;
        IAllocationManager(IAllocationManager&&)      = delete;
        IAllocationManager& operator=(IAllocationManager const&) = delete;
        IAllocationManager& operator=(IAllocationManager&&) = delete;

        virtual ~IAllocationManager();

        // Interface
        //

        /// Abstract member function to allocate an element or sequence of elements. The developer
        /// should not assume continuity of the address segment as this is not guaranteed. Note this
        /// function may throw if allocation is not possible.
        virtual Address allocate(String const& name = "", Index const& count = 1) = 0;

        /// Abstract member function to release a previously allocated function. Note this function may
        /// throw if an invalid address is passed.
        virtual void release(Address const& address) = 0;

        /// Abstract member function to reset the allocation manager. This function clears all allocations
        /// and resets all statistics.
        virtual void reset() = 0;

        // Statistics
        //

        /// Current number of registers in use. This function is used to inquire about the current number
        /// registers/resources in use.
        uint64_t allocationsInUse() const;

        /// Maximum number of registers in use at any one time. The maximum number of registers used at
        /// any one time. As an example of usage, this function is useful to calculate the total number of
        /// qubits required to execute the entry function.
        uint64_t maxAllocationsUsed() const;

      protected:
        IAllocationManager() = default;
        void updateRegistersInUse(uint64_t n);

      private:
        uint64_t registers_in_use_{0};   ///< Used to track the number of registers in use
        uint64_t max_registers_used_{0}; ///< Used to track the max number of registers used
    };

} // namespace quantum
} // namespace microsoft
