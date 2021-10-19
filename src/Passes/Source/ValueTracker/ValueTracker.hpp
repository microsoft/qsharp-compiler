#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"

#include <memory>
#include <unordered_map>

namespace microsoft
{
namespace quantum
{

    class ValueTracker
    {
      public:
        using ValueTrackerPtr = std::shared_ptr<ValueTracker>;
        using OffsetToValue   = std::unordered_map<uint64_t, llvm::Value*>;
        using ValueMap        = std::unordered_map<llvm::Value*, OffsetToValue>;

        static ValueTrackerPtr createNew()
        {
            ValueTrackerPtr ret;
            ret.reset(new ValueTracker());
            return ret;
        }

        void alloc(llvm::Value* address)
        {
            values_[address] = OffsetToValue();
        }

        void store(llvm::Value* address, uint64_t offset, llvm::Value* value)
        {
            values_[address][offset] = value;
        }

        llvm::Value* load(llvm::Value* address, uint64_t offset)
        {
            return values_[address][offset];
        }

      private:
        ValueTracker() = default;
        ValueMap values_{};
    };

} // namespace quantum
} // namespace microsoft
