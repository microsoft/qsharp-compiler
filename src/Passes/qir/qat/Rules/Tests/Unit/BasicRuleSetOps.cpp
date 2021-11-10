// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/DefaultProfileGenerator.hpp"
#include "Rules/Notation/Notation.hpp"
#include "Rules/ReplacementRule.hpp"
#include "Rules/RuleSet.hpp"
#include "TestTools/IrManipulationTestHelper.hpp"
#include "gtest/gtest.h"

#include <functional>
#include <memory>

using namespace microsoft::quantum;

namespace
{
using IrManipulationTestHelperPtr = std::shared_ptr<IrManipulationTestHelper>;
IrManipulationTestHelperPtr newIrManip(std::string const& script)
{
    IrManipulationTestHelperPtr ir_manip = std::make_shared<IrManipulationTestHelper>();

    ir_manip->declareOpaque("Qubit");
    ir_manip->declareOpaque("Result");

    ir_manip->declareFunction("%Qubit* @__quantum__rt__qubit_allocate()");
    ir_manip->declareFunction("void @__quantum__rt__qubit_release(%Qubit*)");
    ir_manip->declareFunction("void @__quantum__qis__h__body(%Qubit*)");

    if (!ir_manip->fromBodyString(script))
    {
        llvm::outs() << ir_manip->getErrorMessage() << "\n";
        exit(-1);
    }
    return ir_manip;
}

} // namespace

// Single allocation with action and then release
TEST(RuleSetTestSuite, BasicOperations)
{
    using namespace microsoft::quantum::notation;

    RuleSet rule_set;
    EXPECT_EQ(rule_set.size(), 0);
    auto deleter = deleteInstruction();

    ReplacementRule rule{callByNameOnly("__quantum__rt__qubit_release_array"), std::move(deleter)};
    auto            ret = std::make_shared<ReplacementRule>(rule);

    rule_set.addRule(ret);

    EXPECT_EQ(rule_set.size(), 1);
    rule_set.clear();
    EXPECT_EQ(rule_set.size(), 0);
}

TEST(RuleSetTestSuite, SetReplacerAndPattern)
{
    using namespace microsoft::quantum::notation;

    auto ir_manip = newIrManip(R"script(
  %qubit = inttoptr i64 0 to %Qubit*
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)    
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        ReplacementRule rule{nullptr, nullptr};
        auto            ret = std::make_shared<ReplacementRule>(rule);
        ret->setReplacer(deleteInstruction());
        ret->setPattern(callByNameOnly("__quantum__rt__qubit_release"));
        rule_set.addRule(ret);
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(std::move(configure_profile));

    EXPECT_TRUE(
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}) ||
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}));
    ir_manip->applyProfile(profile);
    EXPECT_FALSE(
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release(%Qubit* null)"}) ||
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}));
}

TEST(RuleSetTestSuite, NullPattern)
{
    using namespace microsoft::quantum::notation;

    auto ir_manip = newIrManip(R"script(
  %qubit = inttoptr i64 0 to %Qubit*
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)    
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        ReplacementRule rule{nullptr, deleteInstruction()};
        auto            ret = std::make_shared<ReplacementRule>(rule);
        rule_set.addRule(ret);
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(std::move(configure_profile));

    EXPECT_TRUE(
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}) ||
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}));
    ir_manip->applyProfile(profile);

    EXPECT_TRUE(
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release(%Qubit* null)"}) ||
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}));
}

TEST(RuleSetTestSuite, NullReplacer)
{
    using namespace microsoft::quantum::notation;

    auto ir_manip = newIrManip(R"script(
  %qubit = inttoptr i64 0 to %Qubit*
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)    
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        ReplacementRule rule{callByNameOnly("__quantum__rt__qubit_release"), nullptr};
        auto            ret = std::make_shared<ReplacementRule>(rule);
        rule_set.addRule(ret);
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(std::move(configure_profile));

    EXPECT_TRUE(
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}) ||
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}));
    ir_manip->applyProfile(profile);

    EXPECT_TRUE(
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release(%Qubit* null)"}) ||
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}));
}
