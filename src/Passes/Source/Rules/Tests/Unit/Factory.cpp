// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/Factory.hpp"

#include "Llvm/Llvm.hpp"
#include "Passes/ExpandStaticAllocation/ExpandStaticAllocation.hpp"
#include "Passes/QirAllocationAnalysis/QirAllocationAnalysis.hpp"
#include "Passes/TransformationRule/TransformationRule.hpp"
#include "Profiles/RuleSetProfile.hpp"
#include "TestTools/IrManipulationTestHelper.hpp"
#include "gtest/gtest.h"

#include <functional>

using namespace microsoft::quantum;

namespace {
using IrManipulationTestHelperPtr = std::shared_ptr<IrManipulationTestHelper>;
IrManipulationTestHelperPtr newIrManip()
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

  ir_manip->fromBodyString(R"script(
  %leftMessage = call %Qubit* @__quantum__rt__qubit_allocate()
  call void @__quantum__qis__h(%Qubit* %leftMessage)
  call void @__quantum__rt__qubit_release(%Qubit* %leftMessage)

  %0 = call %Result* @__quantum__rt__result_get_zero()
  %1 = call i1 @__quantum__rt__result_equal(%Result* nonnull inttoptr (i64 3 to %Result*), %Result* %0)

  ret i8 0
  )script");

  return ir_manip;
}

}  // namespace

TEST(IrManipulationTestHelperSuite, Teleportation)
{
  auto ir_manip = newIrManip();
  auto profile  = std::make_shared<RuleSetProfile>([](RuleSet &rule_set) {
    auto factory = RuleFactory(rule_set);

    factory.disableReferenceCounting();
  });

  ir_manip->applyProfile(profile);
  llvm::errs() << *ir_manip->module() << "\n";
}
