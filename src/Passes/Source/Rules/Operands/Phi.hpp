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

    class PhiPattern : public IOperandPrototype
    {
      public:
        using String = std::string;

        // Construction of the call pattern by name or move only.
        //

        /// Construction by name.
        PhiPattern() = default;

        /// Copy construction prohibited.
        PhiPattern(PhiPattern const& other) = delete;

        /// Move construction allowed.
        PhiPattern(PhiPattern&& other) = default;

        /// Destructor implementation.
        ~PhiPattern() override;

        // Phi implmenetation of the member functions in IOperandPrototype.
        //

        /// Matches the phi node.
        bool match(Value* instr, Captures& captures) const override;

        /// Creates a copy of itself.
        Child copy() const override;
    };

} // namespace quantum
} // namespace microsoft
