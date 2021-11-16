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

    ir_manip->declareFunction("%String* @__quantum__rt__string_create(i8*)");
    ir_manip->declareFunction("void @__quantum__rt__message(%String*)");
    ir_manip->declareFunction("void @__quantum__rt__string_update_alias_count(%String*, i32)");
    ir_manip->declareFunction("void @__quantum__rt__string_update_reference_count(%String*, i32)");

    if (!ir_manip->fromBodyString(script))
    {
        llvm::outs() << ir_manip->getErrorMessage() << "\n";
        exit(-1);
    }
    return ir_manip;
}

} // namespace

TEST(RuleSetTestSuite, DisablingStrings)
{
    auto ir_manip = newIrManip(R"script(
    %0 = call %String* @__quantum__rt__string_create(i8* null)
    call void @__quantum__rt__string_update_reference_count(%String* %0, i32 1)      
    call void @__quantum__rt__string_update_alias_count(%String* %0, i32 1)    
    call void @__quantum__rt__message(%String* %0)
    call void @__quantum__rt__string_update_alias_count(%String* %0, i32 -1)    
    call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -11)          
  )script");

    auto configure_profile = [](RuleSet& rule_set) {
        auto factory = RuleFactory(rule_set, BasicAllocationManager::createNew(), BasicAllocationManager::createNew());

        factory.disableStringSupport();
    };

    auto profile = std::make_shared<DefaultProfileGenerator>(std::move(configure_profile));

    ir_manip->applyProfile(profile);

    // We expect that the call was removed
    EXPECT_EQ(ir_manip->toBodyInstructions(), IrManipulationTestHelper::Strings{"ret i8 0"});
}
