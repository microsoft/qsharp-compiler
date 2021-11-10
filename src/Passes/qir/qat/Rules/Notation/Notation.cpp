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

        ReplacerFunction deleteInstruction()
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
