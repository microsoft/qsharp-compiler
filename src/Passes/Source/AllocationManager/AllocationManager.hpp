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

    class AllocationManager
    {
      public:
        /// Defines a named register/memory segment with start
        /// position, end position and size.
        struct MemoryMapping
        {
            using Index  = uint64_t;
            using String = std::string;

            String name{""}; ///< Name of the segment, if any given
            Index  index{0}; ///< Index of the allocation
            Index  size{0};  ///< Size of memory segment
            Index  start{0}; ///< Start index of memory segment
            Index  end{0};   ///< Index not included in memory segment
        };

        using Index                = uint64_t;
        using String               = std::string;
        using AllocationManagerPtr = std::shared_ptr<AllocationManager>;
        using Resource             = std::vector<llvm::Value*>;
        using Resources            = std::unordered_map<std::string, Resource>;
        using NameToIndex          = std::unordered_map<String, Index>;
        using Mappings             = std::vector<MemoryMapping>;

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

        /// Allocates a single address.
        Index allocate();

        /// Allocates a name segment of a given size.
        void allocate(String const& name, Index const& size, bool value_only = false);

        /// Gets the offset of a name segment or address.
        Index getOffset(String const& name) const;

        /// Releases unnamed address.
        void release();

        /// Releases the named segment or address.
        void release(String const& name);

        /// Retrieves a named resource.
        Resource& get(String const& name);
        /// @}

      private:
        /// Private constructors
        /// @{
        /// Public construction of this object is only allowed
        /// as a shared pointer. To create a new AllocationManager,
        /// use AllocationManager::createNew().
        AllocationManager() = default;
        /// @}

        /// Memory mapping
        /// @{
        /// Each allocation has a register/memory mapping which
        /// keeps track of the allocation index, the segment size
        /// and its name (if any).
        NameToIndex name_to_index_;
        Mappings    mappings_;
        /// @}

        /// Compile-time resources
        /// @{
        /// Compile-time allocated resources that keeps a pointer
        /// to the corresponding Values.
        Resources resources_;
        /// @}

        Index start_{0};
    };

} // namespace quantum
} // namespace microsoft
