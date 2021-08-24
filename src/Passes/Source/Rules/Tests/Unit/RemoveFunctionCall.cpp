// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Passes/ExpandStaticAllocation/ExpandStaticAllocation.hpp"
#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Profiles/RuleSetProfile.hpp"
#include "Rules/Factory.hpp"
#include "TestTools/IrManipulationTestHelper.hpp"
#include "gtest/gtest.h"

#include "Llvm/Llvm.hpp"

#include <functional>

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

    assert(ir_manip->fromBodyString(script));

    return ir_manip;
}

} // namespace

// Single allocation with action and then release
TEST(RuleSetTestSuite, RemovingFunctionCall)
{
    auto ir_manip = newIrManip(R"script(
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)    
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set);

        factory.removeFunctionCall("__quantum__qis__h__body");
    };

    auto profile = std::make_shared<RuleSetProfile>(std::move(configure_profile));
    ir_manip->applyProfile(profile);

    EXPECT_TRUE(ir_manip->hasInstructionSequence(
        {"%qubit = tail call %Qubit* @__quantum__rt__qubit_allocate()",
         "tail call void @__quantum__rt__qubit_release(%Qubit* %qubit)"}));

    // We expect that the call was removed
    EXPECT_FALSE(ir_manip->hasInstructionSequence({"call void @__quantum__qis__h__body(%Qubit* %qubit)"}));
    EXPECT_FALSE(ir_manip->hasInstructionSequence({"tail call void @__quantum__qis__h__body(%Qubit* %qubit)"}));
}
