#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/IOperandPrototype.hpp"

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    class UnnamedInvokePattern : public IOperandPrototype
    {
      public:
        using String = std::string;

        // Construction
        //

        UnnamedInvokePattern() = default;

        /// Copy construction prohibited.
        UnnamedInvokePattern(UnnamedInvokePattern const& other) = delete;

        /// Move construction allowed.
        UnnamedInvokePattern(UnnamedInvokePattern&& other) = default;

        /// Destructor implementation.
        ~UnnamedInvokePattern() override;

        // Call implementation of the member functions in IOperandPrototype.
        //

        /// Matches the callee by name.
        bool match(Value* instr, Captures& captures) const override;

        /// Creates a copy of itself.
        Child copy() const override;

      private:
    };

} // namespace quantum
} // namespace microsoft
