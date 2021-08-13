#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Operands/Any.hpp"
#include "Rules/Operands/Call.hpp"
#include "Rules/Operands/Instruction.hpp"

#include "Llvm/Llvm.hpp"

#include <unordered_map>
#include <vector>

namespace microsoft
{
namespace quantum
{

    /// Rule that describes a pattern and how to make a replacement of the matched values.
    /// The class contians a OprandPrototype which is used to test whether an LLVM IR value
    /// follows a specific pattern. The class also holds a function pointer to logic that
    /// allows replacement of the specified value.
    class ReplacementRule
    {
      public:
        using Captures            = OperandPrototype::Captures;
        using Instruction         = llvm::Instruction;
        using Value               = llvm::Value;
        using OperandPrototypePtr = std::shared_ptr<OperandPrototype>;
        using Builder             = llvm::IRBuilder<>;
        using Replacements        = std::vector<std::pair<Value*, Value*>>;
        using ReplaceFunction     = std::function<bool(Builder&, Value*, Captures&, Replacements&)>;

        /// Constructorss and destructors
        /// @{
        ReplacementRule() = default;
        ReplacementRule(OperandPrototypePtr&& pattern, ReplaceFunction&& replacer);
        /// @}

        /// Rule configuration
        /// @{

        /// Sets the pattern describing logic to be replaced.
        void setPattern(OperandPrototypePtr&& pattern);

        /// Sets the replacer logic which given a successful match will perform
        /// a replacement on the IR.
        void setReplacer(ReplaceFunction const& replacer);
        /// @}

        /// Operation
        /// @{
        /// Tests whether a given value matches the rule pattern and store captures.
        /// The function returns true if the match was successful in which case captures
        /// are recorded.
        bool match(Value* value, Captures& captures) const;

        /// Invokes the replacer given a matched value and its corresponding captures
        //
        bool replace(Builder& builder, Value* value, Captures& captures, Replacements& replacements) const;
        /// @}
      private:
        OperandPrototypePtr pattern_{nullptr};
        ReplaceFunction     replacer_{nullptr};
    };

} // namespace quantum
} // namespace microsoft
