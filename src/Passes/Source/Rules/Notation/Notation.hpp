#pragma once
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Notation/Call.ipp"
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

        using OperandPrototypePtr = std::shared_ptr<OperandPrototype>;

        class Capture
        {
          public:
            Capture(std::string const& name);
            OperandPrototypePtr operator=(OperandPrototypePtr const& other);

          private:
            std::string name_{};
        };

        /// @{
        template <typename... Args> OperandPrototypePtr Call(std::string const& name, Args... args);
        OperandPrototypePtr                             CallByNameOnly(std::string const& name);
        OperandPrototypePtr                             BitCast(OperandPrototypePtr arg);
        OperandPrototypePtr Branch(OperandPrototypePtr cond, OperandPrototypePtr arg1, OperandPrototypePtr arg2);
        OperandPrototypePtr Load(OperandPrototypePtr arg);
        OperandPrototypePtr Store(OperandPrototypePtr target, OperandPrototypePtr value);
        /// @}

        /// @{
        static std::shared_ptr<AnyPattern> _ = std::make_shared<AnyPattern>();
        /// @}

        /// @{
        std::function<bool(
            ReplacementRule::Builder&,
            ReplacementRule::Value*,
            ReplacementRule::Captures&,
            ReplacementRule::Replacements&)>
        deleteInstruction();

        /// @}

        Capture operator""_cap(char const* name, std::size_t);

    } // namespace notation
} // namespace quantum
} // namespace microsoft
