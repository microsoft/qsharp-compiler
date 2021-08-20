// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Passes/ExpandStaticAllocation/ExpandStaticAllocation.hpp"
#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Profiles/RuleSetProfile.hpp"
#include "Rules/Factory.hpp"
#include "TestTools/IrManipulationTestHelper.hpp"
#include "gtest/gtest.h"

#include <functional>

using namespace microsoft::quantum;

namespace {
using IrManipulationTestHelperPtr = std::shared_ptr<IrManipulationTestHelper>;
IrManipulationTestHelperPtr newIrManip(std::string const &script)
{
  IrManipulationTestHelperPtr ir_manip = std::make_shared<IrManipulationTestHelper>();

  ir_manip->declareOpaque("Qubit");
  ir_manip->declareOpaque("Result");

  ir_manip->declareFunction("%Qubit* @__quantum__rt__qubit_allocate()");
  ir_manip->declareFunction("void @__quantum__rt__qubit_release(%Qubit*)");
  ir_manip->declareFunction("void @__quantum__qis__h(%Qubit*)");

  assert(ir_manip->fromBodyString(script));

  return ir_manip;
}

}  // namespace

// Single allocation with action and then release
TEST(RuleSetTestSuite, AllocationActionRelease)
{
  auto ir_manip = newIrManip(R"script(
  %qubit = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release(%Qubit* %qubit)    
  )script");

  auto configure_profile = [](RuleSet &rule_set) {
    auto factory = RuleFactory(rule_set);

    factory.useStaticQubitAllocation();
  };

  auto profile = std::make_shared<RuleSetProfile>(std::move(configure_profile));

  ir_manip->applyProfile(profile);

  EXPECT_TRUE(ir_manip->hasInstructionSequence(
      {"%qubit = inttoptr i64 0 to %Qubit*", "tail call void @__quantum__qis__h(%Qubit* %qubit)"}));

  llvm::errs() << *ir_manip->module() << "\n";
}

// Scenario 2 - Multiple sequential allocations
TEST(RuleSetTestSuite, MultipleAllocationsNoRelease)
{
  auto ir_manip = newIrManip(R"script(
  %qubit1 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit2 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit3 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit4 = call %Qubit* @__quantum__rt__qubit_allocate()
  %qubit5 = call %Qubit* @__quantum__rt__qubit_allocate()
  )script");

  auto profile = std::make_shared<RuleSetProfile>([](RuleSet &rule_set) {
    auto factory = RuleFactory(rule_set);

    factory.useStaticQubitAllocation();
  });

  ir_manip->applyProfile(profile);

  // Checking that static allocations happened
  EXPECT_TRUE(ir_manip->hasInstructionSequence({
      "%qubit1 = inttoptr i64 0 to %Qubit*",
      "%qubit2 = inttoptr i64 1 to %Qubit*",
      "%qubit3 = inttoptr i64 2 to %Qubit*",
      "%qubit4 = inttoptr i64 3 to %Qubit*",
      "%qubit5 = inttoptr i64 4 to %Qubit*",
  }));

  // Checking that dynamic allocations were removed
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit1 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit2 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit3 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit4 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit5 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
}

// Scenario 3 -  Allocate, release - multiple times
TEST(RuleSetTestSuite, AllocateReleaseMultipleTimes)
{
  auto ir_manip = newIrManip(R"script(
  %qubit1 = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__rt__qubit_release(%Qubit* %qubit1)    
  %qubit2 = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__rt__qubit_release(%Qubit* %qubit2)    
  %qubit3 = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__rt__qubit_release(%Qubit* %qubit3)    
  %qubit4 = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__rt__qubit_release(%Qubit* %qubit4)  
  %qubit5 = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__rt__qubit_release(%Qubit* %qubit4)  
  )script");

  auto profile = std::make_shared<RuleSetProfile>([](RuleSet &rule_set) {
    auto factory = RuleFactory(rule_set);

    factory.useStaticQubitAllocation();
  });

  ir_manip->applyProfile(profile);

  // TODO: Ideally these shoulw all have the same id, however,
  // this require more thought with regards to the qubit allocation
  EXPECT_TRUE(ir_manip->hasInstructionSequence({
      "%qubit1 = inttoptr i64 0 to %Qubit*",
      "%qubit2 = inttoptr i64 1 to %Qubit*",
      "%qubit3 = inttoptr i64 2 to %Qubit*",
      "%qubit4 = inttoptr i64 3 to %Qubit*",
      "%qubit5 = inttoptr i64 4 to %Qubit*",
  }));

  // Checking that dynamic allocations were removed
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit1 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit2 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit3 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit4 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
  EXPECT_FALSE(ir_manip->hasInstructionSequence({
      "%qubit5 = call %Qubit* @__quantum__rt__qubit_allocate()",
  }));
}
