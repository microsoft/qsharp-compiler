// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Notation/Notation.hpp"
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
    namespace notation
    {
        /// Replacement function to delete an instruction. This is a shorthand notation for deleting
        /// an instruction that can be used with a custom rule when building a ruleset. This function
        /// can be used with shorthand notation for patterns as follows:
        /// ```c++
        /// addRule({callByNameOnly(name), deleteInstruction()});
        /// ```
        /// to delete the instructions that calls functions with the name `name`.
        std::function<bool(
            ReplacementRule::Builder&,
            ReplacementRule::Value*,
            ReplacementRule::Captures&,
            ReplacementRule::Replacements&)>
        deleteInstruction()
        {
            return [](ReplacementRule::Builder&, ReplacementRule::Value* val, ReplacementRule::Captures&,
                      ReplacementRule::Replacements& replacements) {
                replacements.push_back({llvm::dyn_cast<llvm::Instruction>(val), nullptr});
                return true;
            };
        }

    } // namespace notation
} // namespace quantum
} // namespace microsoft
