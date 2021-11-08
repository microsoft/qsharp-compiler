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

    class StorePattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~StorePattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class LoadPattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~LoadPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class BitCastPattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~BitCastPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class IntToPtrPattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~IntToPtrPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class ConstIntPattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~ConstIntPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class BranchPattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~BranchPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class SelectPattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~SelectPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class BasicBlockPattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~BasicBlockPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

    class SwitchPattern : public IOperandPrototype
    {
      public:
        using IOperandPrototype::IOperandPrototype;
        ~SwitchPattern() override;
        bool  match(Value* instr, Captures& captures) const override;
        Child copy() const override;
    };

} // namespace quantum
} // namespace microsoft
