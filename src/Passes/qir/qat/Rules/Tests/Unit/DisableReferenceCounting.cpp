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

    ir_manip->declareOpaque("Qubit");
    ir_manip->declareOpaque("Result");
    ir_manip->declareOpaque("String");
    ir_manip->declareOpaque("Array");

    ir_manip->declareFunction("%Array* @__quantum__rt__array_create_1d(i32, i64)");
    ir_manip->declareFunction("%String* @__quantum__rt__string_create(i8*)");
    ir_manip->declareFunction("%Result* @__quantum__qis__m__body(%Qubit*)");

    ir_manip->declareFunction("void @__quantum__rt__array_update_reference_count(%Array*, i32)");
    ir_manip->declareFunction("void @__quantum__rt__string_update_reference_count(%String*, i32)");
    ir_manip->declareFunction("void @__quantum__rt__result_update_reference_count(%Result*, i32)");

    if (!ir_manip->fromBodyString(script))
    {
        llvm::outs() << ir_manip->getErrorMessage() << "\n";
        exit(-1);
    }
    return ir_manip;
}

} // namespace

// Single allocation with action and then release
TEST(RuleSetTestSuite, DISABLED_DisablingArrayhReferenceCounting)
{
    auto ir_manip = newIrManip(R"script(
    %0 = call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)
    call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)    
    call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)    
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set, BasicAllocationManager::createNew(), BasicAllocationManager::createNew());

        factory.disableReferenceCounting();
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(std::move(configure_profile));

    // We expect that the calls are there initially
    EXPECT_TRUE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)"}));
    EXPECT_TRUE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)"}));

    ir_manip->applyProfile(profile);

    EXPECT_TRUE(
        ir_manip->hasInstructionSequence({"%0 = tail call %Array* @__quantum__rt__array_create_1d(i32 8, i64 2)"}));

    // We expect that the call was removed
    EXPECT_FALSE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 1)"}));
    EXPECT_FALSE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__array_update_reference_count(%Array* %0, i32 -1)"}));
}

TEST(RuleSetTestSuite, DisablingStringReferenceCounting)
{
    auto ir_manip = newIrManip(R"script(
    %0 = call %String* @__quantum__rt__string_create(i8* null)
    call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)    
    call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)    
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set, BasicAllocationManager::createNew(), BasicAllocationManager::createNew());

        factory.disableReferenceCounting();
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(std::move(configure_profile));

    // We expect that the calls are there initially
    EXPECT_TRUE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)"}));
    EXPECT_TRUE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)"}));

    ir_manip->applyProfile(profile);

    EXPECT_TRUE(ir_manip->hasInstructionSequence({"%0 = tail call %String* @__quantum__rt__string_create(i8* null)"}));

    // We expect that the call was removed
    EXPECT_FALSE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)"}));
    EXPECT_FALSE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)"}));
}

TEST(RuleSetTestSuite, DisablingResultReferenceCounting)
{
    auto ir_manip = newIrManip(R"script(
    %0 = call %Result* @__quantum__qis__m__body(%Qubit* null)
    call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 1)    
    call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)    
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set, BasicAllocationManager::createNew(), BasicAllocationManager::createNew());

        factory.disableReferenceCounting();
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(std::move(configure_profile));

    // We expect that the calls are there initially
    EXPECT_TRUE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 1)"}));
    EXPECT_TRUE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)"}));

    ir_manip->applyProfile(profile);

    EXPECT_TRUE(ir_manip->hasInstructionSequence({"%0 = tail call %Result* @__quantum__qis__m__body(%Qubit* null)"}));

    // We expect that the call was removed
    EXPECT_FALSE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 1)"}));
    EXPECT_FALSE(
        ir_manip->hasInstructionSequence(
            {"call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)"}) ||
        ir_manip->hasInstructionSequence(
            {"tail call void @__quantum__rt__result_update_reference_count(%Result* %0, i32 -1)"}));
}
