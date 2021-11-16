// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Generators/DefaultProfileGenerator.hpp"
#include "Generators/LlvmPassesConfiguration.hpp"
#include "Rules/FactoryConfig.hpp"
#include "TestTools/IrManipulationTestHelper.hpp"
#include "TransformationRulesPass/TransformationRulesPassConfiguration.hpp"
#include "gtest/gtest.h"

using namespace microsoft::quantum;
using GeneratorPtr = std::shared_ptr<DefaultProfileGenerator>;
namespace
{
class ExposedDefaultProfileGenerator : public DefaultProfileGenerator
{
  public:
    using DefaultProfileGenerator::createGenerationModulePassManager;
    using DefaultProfileGenerator::createValidationModulePass;
    using DefaultProfileGenerator::DefaultProfileGenerator;
};

class TestAnalysis
{
  public:
    TestAnalysis(TestAnalysis const&) = delete;
    TestAnalysis(TestAnalysis&&)      = default;
    ~TestAnalysis()                   = default;
    explicit TestAnalysis(bool debug = false)
      : loop_analysis_manager_{debug}
      , function_analysis_manager_{debug}
      , gscc_analysis_manager_{debug}
      , module_analysis_manager_{debug}
    {

        // Creating a full pass builder and registering each of the
        // components to make them accessible to the developer.
        pass_builder_.registerModuleAnalyses(module_analysis_manager_);
        pass_builder_.registerCGSCCAnalyses(gscc_analysis_manager_);
        pass_builder_.registerFunctionAnalyses(function_analysis_manager_);
        pass_builder_.registerLoopAnalyses(loop_analysis_manager_);

        pass_builder_.crossRegisterProxies(
            loop_analysis_manager_, function_analysis_manager_, gscc_analysis_manager_, module_analysis_manager_);
    }

    llvm::PassBuilder& passBuilder()
    {
        return pass_builder_;
    }

    llvm::LoopAnalysisManager& loopAnalysisManager()
    {
        return loop_analysis_manager_;
    }

    llvm::FunctionAnalysisManager& functionAnalysisManager()
    {
        return function_analysis_manager_;
    }

    llvm::CGSCCAnalysisManager& gsccAnalysisManager()
    {
        return gscc_analysis_manager_;
    }

    llvm::ModuleAnalysisManager& moduleAnalysisManager()
    {
        return module_analysis_manager_;
    }

  private:
    // Objects used to run a set of passes
    //

    llvm::PassBuilder             pass_builder_;
    llvm::LoopAnalysisManager     loop_analysis_manager_;
    llvm::FunctionAnalysisManager function_analysis_manager_;
    llvm::CGSCCAnalysisManager    gscc_analysis_manager_;
    llvm::ModuleAnalysisManager   module_analysis_manager_;
};
} // namespace

TEST(GeneratorsTestSuite, ConfigureFunction)
{
    Profile  profile{"test", false};
    uint64_t call_count{0};
    auto     configure = [&call_count](RuleSet&) { ++call_count; };
    auto     generator = std::make_shared<ExposedDefaultProfileGenerator>(configure);

    TestAnalysis test;

    auto module_pass_manager =
        generator->createGenerationModulePassManager(profile, llvm::PassBuilder::OptimizationLevel::O0, false);

    EXPECT_EQ(call_count, 1);
    EXPECT_TRUE(generator->ruleTransformationConfig().isDisabled());
    EXPECT_TRUE(generator->llvmPassesConfig().isDisabled());
}

TEST(GeneratorsTestSuite, ConfigurationManager)
{
    Profile               profile{"test", false};
    auto                  generator             = std::make_shared<ExposedDefaultProfileGenerator>();
    ConfigurationManager& configuration_manager = generator->configurationManager();
    configuration_manager.addConfig<FactoryConfiguration>();

    TestAnalysis test;

    auto module_pass_manager =
        generator->createGenerationModulePassManager(profile, llvm::PassBuilder::OptimizationLevel::O0, false);

    EXPECT_EQ(generator->ruleTransformationConfig(), TransformationRulesPassConfiguration());
    EXPECT_EQ(generator->llvmPassesConfig(), LlvmPassesConfiguration());
    EXPECT_FALSE(generator->ruleTransformationConfig().isDisabled());
}
