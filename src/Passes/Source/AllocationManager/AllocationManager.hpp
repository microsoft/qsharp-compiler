#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

#include <memory>
#include <string>
#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{
    // TODO(QAT-private-issue-35): work out similarities and differences between resource allocation at
    // runtime and compile time to make common interface and/or implementation depending on what is most
    // suited.

    /// AllocationManager is a simple qubit and results allocator that can be used at compile-time.
    /// It is based on an assumption that all qubit allocating function calls are inlined and that
    /// qubits/results can be allocated with strictly growing IDs.
    class AllocationManager
    {
      public:
        /// Defines a named register/memory segment with start
        /// position, end position and size.
        struct MemoryMapping
        {
            using Address = uint64_t;
            using Index   = uint64_t;
            using String  = std::string;

            String  name{""}; ///< Name of the segment, if any given
            Index   index{0}; ///< Index of the allocation
            Index   size{0};  ///< Size of memory segment
            Address start{0}; ///< Start index of memory segment
            Address end{0};   ///< Index not included in memory segment
        };

        using Address              = uint64_t;
        using Index                = uint64_t;
        using String               = std::string;
        using AllocationManagerPtr = std::shared_ptr<AllocationManager>;
        using Resource             = std::vector<llvm::Value*>;
        using Resources            = std::unordered_map<std::string, Resource>;
        using NameToIndex          = std::unordered_map<String, Index>;
        using AddressToIndex       = std::unordered_map<Address, Index>;
        using Mappings             = std::unordered_map<Index, MemoryMapping>;

        /// Construction only allowed using smart pointer allocation through static functions.
        /// Constructors are private to prevent
        /// @{

        /// Creates a new allocation manager. The manager is kept
        /// as a shared pointer to enable allocation accross diffent
        /// passes and/or replacement rules.
        static AllocationManagerPtr createNew();
        /// @}

        /// Allocation and release functions
        /// @{

        /// Allocates a possibly named segment of a given size. Calling allocate without and
        /// arguments allocates a single anonymous resource and returns the address. In case
        /// of a larger segment, the function returns the address pointing to the first element.
        /// Allocation is garantueed to be sequential. Note that this assumption may change in the
        /// future and to be future proof, please use AllocationManager::getAddress().
        Address allocate(String const& name = "", Index const& size = 1);

        /// Gets the Address of a named segment or address. Given a named resource segment, this
        /// function returns the address of the first element in the
        Address getOffset(String const& name) const;

        /// Gets the Address of a named segments n'th element.
        Address getAddress(String const& name, Index const& n) const;

        /// Gets the Address of n'th element in a segment given the segments address.
        Address getAddress(Address const& address, Index const& n) const;

        /// Releases the named segment.
        void release(String const& name);

        /// Releases the segment by address.
        void release(Address const& address);

        /// @}

      private:
        /// Private constructors
        /// @{
        /// Public construction of this object is only allowed
        /// as a shared pointer. To create a new AllocationManager,
        /// use AllocationManager::createNew().
        AllocationManager() = default;
        /// @}

        /// Variables used for mode_ == NeverReuse
        /// @{

        /// Variable to keep track of the next qubit to be allocated.
        Index next_qubit_index_{0};
        /// @}

        /// Memory mapping
        /// @{
        /// Each allocation has a register/memory mapping which
        /// keeps track of the allocation index, the segment size
        /// and its name (if any).
        NameToIndex    name_to_index_;
        AddressToIndex address_to_index_;
        Mappings       mappings_;
        /// @}

        Index allocation_index_{0};
    };

} // namespace quantum
} // namespace microsoft
