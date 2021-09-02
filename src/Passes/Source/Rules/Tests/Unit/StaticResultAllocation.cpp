// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Llvm/Llvm.hpp"
#include "Passes/ExpandStaticAllocation/ExpandStaticAllocation.hpp"
#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"
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

  ir_manip->declareFunction("%Qubit* @__non_standard_allocator()");
  ir_manip->declareFunction("i8* @__non_standard_int_allocator()");
  ir_manip->declareFunction("%Result* @__quantum__qis__m__body(%Qubit*)");

  if (!ir_manip->fromBodyString(script))
  {
    llvm::errs() << ir_manip->getErrorMessage() << "\n";
    exit(-1);
  }
  return ir_manip;
}

}  // namespace

// Single allocation with action and then release
TEST(RuleSetTestSuite, ResultTranslatedTo)
{
  auto ir_manip = newIrManip(R"script(
  %result1 = call %Result* @__quantum__qis__m__body(%Qubit* null)
  %result2 = call %Result* @__quantum__qis__m__body(%Qubit* null)
  %result3 = call %Result* @__quantum__qis__m__body(%Qubit* null)
  %result4 = call %Result* @__quantum__qis__m__body(%Qubit* null)
  %result5 = call %Result* @__quantum__qis__m__body(%Qubit* null)    
  )script");

  auto configure_profile = [](RuleSet &rule_set) {
    auto factory = RuleFactory(rule_set);

    factory.useStaticResultAllocation();
  };

  auto profile = std::make_shared<RuleSetProfile>(std::move(configure_profile));
  ir_manip->applyProfile(profile);

  EXPECT_TRUE(ir_manip->hasInstructionSequence({
      "%result1 = inttoptr i64 0 to %Result*",
      "call void @__quantum__qis__mz__body(%Qubit* null, %Result* %result1)",
      "%result2 = inttoptr i64 1 to %Result*",
      "call void @__quantum__qis__mz__body(%Qubit* null, %Result* %result2)",
      "%result3 = inttoptr i64 2 to %Result*",
      "call void @__quantum__qis__mz__body(%Qubit* null, %Result* %result3)",
      "%result4 = inttoptr i64 3 to %Result*",
      "call void @__quantum__qis__mz__body(%Qubit* null, %Result* %result4)",
      "%result5 = inttoptr i64 4 to %Result*",
      "call void @__quantum__qis__mz__body(%Qubit* null, %Result* %result5)",

  }));

  EXPECT_FALSE(ir_manip->hasInstructionSequence({
                   "%result1 = call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }) ||
               ir_manip->hasInstructionSequence({
                   "%result1 = tail call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }));

  EXPECT_FALSE(ir_manip->hasInstructionSequence({
                   "%result2 = call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }) ||
               ir_manip->hasInstructionSequence({
                   "%result2 = tail call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }));

  EXPECT_FALSE(ir_manip->hasInstructionSequence({
                   "%result3 = call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }) ||
               ir_manip->hasInstructionSequence({
                   "%result3 = tail call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }));

  EXPECT_FALSE(ir_manip->hasInstructionSequence({
                   "%result4 = call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }) ||
               ir_manip->hasInstructionSequence({
                   "%result4 = tail call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }));

  EXPECT_FALSE(ir_manip->hasInstructionSequence({
                   "%result5 = call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }) ||
               ir_manip->hasInstructionSequence({
                   "%result5 = tail call %Result* @__quantum__qis__m__body(%Qubit* null)",
               }));
}
