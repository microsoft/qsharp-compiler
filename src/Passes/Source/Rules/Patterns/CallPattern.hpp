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

    class CallPattern : public IOperandPrototype
    {
      public:
        using String = std::string;

        // Construction of the call pattern by name or move only.
        //

        /// Construction by name.
        explicit CallPattern(String const& name);

        /// Copy construction prohibited.
        CallPattern(CallPattern const& other) = delete;

        /// Move construction allowed.
        CallPattern(CallPattern&& other) = default;

        /// Destructor implementation.
        ~CallPattern() override;

        // Call implmenetation of the member functions in IOperandPrototype.
        //

        /// Matches the callee by name.
        bool match(Value* instr, Captures& captures) const override;

        /// Creates a copy of itself.
        Child copy() const override;

      private:
        String name_{}; ///< Name of the callee to match against.
    };

} // namespace quantum
} // namespace microsoft
