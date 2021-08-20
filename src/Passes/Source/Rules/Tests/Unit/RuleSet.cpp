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

    ir_manip->declareFunction("i1 @__quantum__rt__result_equal(%Result*, %Result*)");
    ir_manip->declareFunction("%Qubit* @__quantum__rt__qubit_allocate()");
    ir_manip->declareFunction("void @__quantum__rt__qubit_release(%Qubit*)");
    ir_manip->declareFunction("void @__quantum__qis__h(%Qubit*)");
    ir_manip->declareFunction("%Result* @__quantum__rt__result_get_zero()");
    ir_manip->declareFunction("void @__quantum__qis__mz__body(%Qubit*, %Result*)");

    assert(ir_manip->fromBodyString(script));

    return ir_manip;
}

} // namespace

TEST(RuleSetTestSuite, StaticQubitAllocation)
{
    // Scenario 1 - Simple allocation, action, release
    {
        auto ir_manip = newIrManip(R"script(
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)    
  )script");

        auto profile = std::make_shared<RuleSetProfile>([](RuleSet& rule_set) {
            auto factory = RuleFactory(rule_set);

            factory.useStaticQubitAllocation();
        });

        ir_manip->applyProfile(profile);

        EXPECT_TRUE(ir_manip->hasInstructionSequence(
            {"%qubit = inttoptr i64 0 to %Qubit*", "tail call void @__quantum__qis__h(%Qubit* %qubit)"}));
    }

    // Scenario 2 - Multiple allocations, action, release
    {
        auto ir_manip = newIrManip(R"script(
  %qubit1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit5 = call %Qubit* @__quantum__rt__qubit_allocate()
  )script");

        auto profile = std::make_shared<RuleSetProfile>([](RuleSet& rule_set) {
            auto factory = RuleFactory(rule_set);

            factory.useStaticQubitAllocation();
        });

        ir_manip->applyProfile(profile);

        EXPECT_TRUE(ir_manip->hasInstructionSequence({
            "%qubit1 = inttoptr i64 0 to %Qubit*",
            "%qubit2 = inttoptr i64 1 to %Qubit*",
            "%qubit3 = inttoptr i64 2 to %Qubit*",
            "%qubit4 = inttoptr i64 3 to %Qubit*",
            "%qubit5 = inttoptr i64 4 to %Qubit*",
        }));

        llvm::errs() << *ir_manip->module() << "\n";
    }
}
