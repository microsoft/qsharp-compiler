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

    ir_manip->declareOpaque("Array");
    ir_manip->declareOpaque("Qubit");
    ir_manip->declareOpaque("Result");

    ir_manip->declareFunction("%Array* @__quantum__rt__qubit_allocate_array(i64)");
    ir_manip->declareFunction("i8* @__quantum__rt__array_get_element_ptr_1d(%Array*, i64)");
    ir_manip->declareFunction("void @__quantum__qis__h__body(%Qubit*)");

    // __quantum__rt__qubit_allocate_array
    // __quantum__rt__array_get_element_ptr_1d

    assert(ir_manip->fromBodyString(script));

    return ir_manip;
}

} // namespace

/*
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %leftPreshared, i64 0)
  %1 = bitcast i8* %0 to i64*
  store i64 1, i64* %1, align 4
*/

// Single allocation with action and then release
TEST(RuleSetTestSuite, StaticQubitArrayAllocationOffsets)
{
    auto ir_manip = newIrManip(R"script(
  %array1 = call %Array* @__quantum__rt__qubit_allocate_array(i64 2) ; offset 0
  %array2 = call %Array* @__quantum__rt__qubit_allocate_array(i64 3) ; offset 2
  %array3 = call %Array* @__quantum__rt__qubit_allocate_array(i64 5) ; offset 5
  %array4 = call %Array* @__quantum__rt__qubit_allocate_array(i64 9) ; offset 10
  %array5 = call %Array* @__quantum__rt__qubit_allocate_array(i64 14) ; offset 19
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set);
        factory.useStaticQubitArrayAllocation();
    };

    auto profile = std::make_shared<RuleSetProfile>(std::move(configure_profile));
    ir_manip->applyProfile(profile);
    llvm::errs() << *ir_manip->module() << "\n";
    EXPECT_TRUE(ir_manip->hasInstructionSequence(
        {"%array1 = inttoptr i64 0 to %Array*", "%array2 = inttoptr i64 2 to %Array*",
         "%array3 = inttoptr i64 5 to %Array*", "%array4 = inttoptr i64 10 to %Array*",
         "%array5 = inttoptr i64 19 to %Array*"}));
}

TEST(RuleSetTestSuite, StaticQubitArrayAllocationGetPtr)
{
    auto ir_manip = newIrManip(R"script(
  %array1 = call %Array* @__quantum__rt__qubit_allocate_array(i64 2) 
  %1 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array1, i64 0)
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set);
        factory.useStaticQubitArrayAllocation();
    };

    auto profile = std::make_shared<RuleSetProfile>(std::move(configure_profile));
    ir_manip->applyProfile(profile);
    llvm::errs() << *ir_manip->module() << "\n";
    EXPECT_TRUE(ir_manip->hasInstructionSequence(
        {"%array1 = inttoptr i64 0 to %Array*", "%array2 = inttoptr i64 2 to %Array*",
         "%array3 = inttoptr i64 5 to %Array*", "%array4 = inttoptr i64 10 to %Array*",
         "%array5 = inttoptr i64 19 to %Array*"}));
}
