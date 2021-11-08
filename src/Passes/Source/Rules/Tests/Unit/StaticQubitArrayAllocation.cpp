// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/DefaultProfileGenerator.hpp"
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
    ir_manip->declareFunction("void @__quantum__rt__qubit_release_array(%Array*)");

    if (!ir_manip->fromBodyString(script))
    {
        llvm::outs() << ir_manip->getErrorMessage() << "\n";
        exit(-1);
    }

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
        auto factory = RuleFactory(rule_set, BasicAllocationManager::createNew(), BasicAllocationManager::createNew());
        factory.useStaticQubitArrayAllocation();
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(
        std::move(configure_profile), TransformationRulesPassConfiguration::createDisabled(),
        LlvmPassesConfiguration::createDisabled());
    ir_manip->applyProfile(profile);

    EXPECT_TRUE(ir_manip->hasInstructionSequence(
        {"%array1 = inttoptr i64 0 to %Array*", "%array2 = inttoptr i64 2 to %Array*",
         "%array3 = inttoptr i64 5 to %Array*", "%array4 = inttoptr i64 10 to %Array*",
         "%array5 = inttoptr i64 19 to %Array*"}));
}

TEST(RuleSetTestSuite, StaticQubitArrayAllocationGetPtr)
{
    auto ir_manip = newIrManip(R"script(
  %array1 = call %Array* @__quantum__rt__qubit_allocate_array(i64 10) 
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array1, i64 7)
  %1 = bitcast i8* %0 to %Qubit**
  %qubit = load %Qubit*, %Qubit** %1, align 8
  call void @__quantum__qis__h__body(%Qubit* %qubit)
  call void @__quantum__rt__qubit_release_array(%Array* %array1)
  )script");

    // TODO(tfr): Possibly the "correct" way to deal with this is to
    // do a more granular approach, translating __quantum__rt__array_get_element_ptr_1d
    // int to a constant i8*. For discussion with team. A good example is
    //
    // %array1 = call %Array* @__quantum__rt__qubit_allocate_array(i64 10)
    // %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array1, i64 7)
    // %1 = bitcast i8* %0 to %Qubit**
    // %qubit = load %Qubit*, %Qubit** %1, align 8
    // ;;; call @__quantum__qis__h__body(%Qubit* %qubit)  < Note this instruction is missing
    //
    // LLVM will optimise the two last instructions away even at O0 as they are not used.
    // Consequently the pattern fails.

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set, BasicAllocationManager::createNew(), BasicAllocationManager::createNew());
        factory.useStaticQubitArrayAllocation();
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(
        std::move(configure_profile), TransformationRulesPassConfiguration::createDisabled(),
        LlvmPassesConfiguration::createDisabled());

    ir_manip->applyProfile(profile);

    EXPECT_TRUE(ir_manip->hasInstructionSequence(
        {"%array1 = inttoptr i64 0 to %Array*", "%qubit = inttoptr i64 7 to %Qubit*"}));

    EXPECT_FALSE(
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release_array(%Array* %array1)"}) ||
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release_array(%Array* %array1)"}));

    EXPECT_FALSE(
        ir_manip->hasInstructionSequence({"call %Array* @__quantum__rt__qubit_allocate_array(i64 10)"}) ||
        ir_manip->hasInstructionSequence({"tail call %Array* @__quantum__rt__qubit_allocate_array(i64 10)"}));

    EXPECT_FALSE(
        ir_manip->hasInstructionSequence(
            {"call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array1, i64 7)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array1, i64 7)"}));
}

TEST(RuleSetTestSuite, StaticQubitArrayAllocationAdvanced)
{
    auto ir_manip = newIrManip(R"script(
  %array1 = call %Array* @__quantum__rt__qubit_allocate_array(i64 10) 
  %array2 = call %Array* @__quantum__rt__qubit_allocate_array(i64 7) 
  %0 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array1, i64 7)
  %1 = call i8* @__quantum__rt__array_get_element_ptr_1d(%Array* %array2, i64 3)
  %2 = bitcast i8* %0 to %Qubit**
  %3 = bitcast i8* %1 to %Qubit**  
  %qubit1 = load %Qubit*, %Qubit** %2, align 8
  %qubit2 = load %Qubit*, %Qubit** %3, align 8
  call void @__quantum__qis__h__body(%Qubit* %qubit1)
  call void @__quantum__qis__h__body(%Qubit* %qubit2)
  call void @__quantum__rt__qubit_release_array(%Array* %array1)
  call void @__quantum__rt__qubit_release_array(%Array* %array2)
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set, BasicAllocationManager::createNew(), BasicAllocationManager::createNew());
        factory.useStaticQubitArrayAllocation();
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(
        std::move(configure_profile), TransformationRulesPassConfiguration::createDisabled(),
        LlvmPassesConfiguration::createDisabled());

    ir_manip->applyProfile(profile);

    EXPECT_TRUE(ir_manip->hasInstructionSequence(
        {"%array1 = inttoptr i64 0 to %Array*", "%qubit1 = inttoptr i64 7 to %Qubit*"}));

    EXPECT_TRUE(ir_manip->hasInstructionSequence(
        {"%array2 = inttoptr i64 10 to %Array*", "%qubit2 = inttoptr i64 13 to %Qubit*"}));

    EXPECT_FALSE(
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release_array(%Array* %array1)"}) ||
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release_array(%Array* %array1)"}));

    EXPECT_FALSE(
        ir_manip->hasInstructionSequence({"call void @__quantum__rt__qubit_release_array(%Array* %array2)"}) ||
        ir_manip->hasInstructionSequence({"tail call void @__quantum__rt__qubit_release_array(%Array* %array2)"}));
}
