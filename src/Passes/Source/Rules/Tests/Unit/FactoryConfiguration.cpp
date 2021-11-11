// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Rules/FactoryConfig.hpp"
#include "gtest/gtest.h"

#include <functional>

using namespace microsoft::quantum;

// Single allocation with action and then release
TEST(RuleSetTestSuite, FactoryConfiguration)
{
    FactoryConfiguration c1 = FactoryConfiguration::createDisabled();
    EXPECT_TRUE(c1.isDisabled());

    FactoryConfiguration c2{};
    EXPECT_TRUE(c2.isDefault());
}
