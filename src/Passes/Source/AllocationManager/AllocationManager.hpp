#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/IAllocationManager.hpp"

#include "Llvm/Llvm.hpp"

#include <memory>
#include <string>
#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{
    /// AllocationManager is a simple qubit and results allocator that can be used at compile-time.
    /// It is based on an assumption that all qubit allocating function calls are inlined and that
    /// qubits/results can be allocated with strictly growing IDs.
    class BasicAllocationManager : public IAllocationManager
    {
      public:
        /// Defines a named register/memory segment with start
        /// position, end position and size.
        struct MemoryMapping
        {
            String  name{""}; ///< Name of the segment, if any given
            Index   index{0}; ///< Index of the allocation
            Index   size{0};  ///< Size of memory segment
            Address start{0}; ///< Start index of memory segment
            Address end{0};   ///< Index not included in memory segment
        };

        using Mappings                  = std::vector<MemoryMapping>;              ///< Vector of memory segments
        using BasicAllocationManagerPtr = std::shared_ptr<BasicAllocationManager>; ///< Allocator pointer type

        // Construction only allowed using smart pointer allocation through static functions.
        // Constructors are private to prevent
        //

        /// Creates a new allocation manager. The manager is kept
        /// as a shared pointer to enable allocation accross diffent
        /// passes and/or replacement rules.
        static BasicAllocationManagerPtr createNew();

        // Allocation and release functions
        //

        /// Allocates a possibly named segment of a given size. Calling allocate without and
        /// arguments allocates a single anonymous resource and returns the address. In case
        /// of a larger segment, the function returns the address pointing to the first element.
        /// Allocation is garantueed to be sequential.
        Address allocate(String const& name = "", Index const& size = 1) override;

        /// Releases the segment by address.
        void release(Address const& address) override;

        /// Resets the allocation manager and all its statistics
        void reset() override;

        /// Configuration function to set mode of qubit allocation. If function argument is true,
        /// the allocation manager will reuse qubits.
        void setReuseQubits(bool val);

      private:
        // Private constructors
        //

        /// Public construction of this object is only allowed
        /// as a shared pointer. To create a new AllocationManager,
        /// use AllocationManager::createNew().
        BasicAllocationManager() = default;

        /// Variable to keep track of the next qubit to be allocated.
        Index next_qubit_index_{0};

        // Memory mapping
        //

        /// Each allocation has a register/memory mapping which
        /// keeps track of the allocation index, the segment size
        /// and its name (if any).
        Mappings mappings_{};
        Index    allocation_index_{0};

        // Configuration
        //

        bool reuse_qubits_{true}; ///< Whether or not to reuse qubits
    };

} // namespace quantum
} // namespace microsoft
