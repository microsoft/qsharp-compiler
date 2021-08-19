// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "gtest/gtest.h"

TEST(AllocationManagerTestSuite, LinearAllocationTest)
{
  auto manager = microsoft::quantum::AllocationManager::createNew();

  // Expecting ids to be allocated linearly for single
  // allocations
  EXPECT_TRUE(manager->allocate() == 0);
  EXPECT_TRUE(manager->allocate() == 1);
  EXPECT_TRUE(manager->allocate() == 2);
  EXPECT_TRUE(manager->allocate() == 3);
  EXPECT_TRUE(manager->allocate() == 4);

  // We expect that allocating
  manager->allocate("test", 10);
  EXPECT_TRUE(manager->getOffset("test") == 5);
  EXPECT_TRUE(manager->allocate() == 15);
  manager->allocate("test2", 10);
  EXPECT_TRUE(manager->getOffset("test") == 5);
  EXPECT_TRUE(manager->getOffset("test2") == 10);
}
