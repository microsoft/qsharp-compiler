// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "gtest/gtest.h"

TEST(AllocationManagerTestSuite, LinearAllocationTestReuse)
{
    auto manager = microsoft::quantum::BasicAllocationManager::createNew();
    manager->setReuseRegisters(true);

    // Expecting ids to be allocated linearly for single
    // allocations
    auto q1 = manager->allocate();
    EXPECT_EQ(q1, 0);
    auto q2 = manager->allocate();
    EXPECT_EQ(q2, 1);
    auto q3 = manager->allocate();
    EXPECT_EQ(q3, 2);
    auto q4 = manager->allocate();
    EXPECT_EQ(q4, 3);
    auto q5 = manager->allocate();
    EXPECT_EQ(q5, 4);

    // We expect that allocating
    auto arr1 = manager->allocate("test", 10);
    EXPECT_EQ(arr1, 5);
    auto arr2 = manager->allocate("test2", 10);
    EXPECT_EQ(arr2, 15);

    // Testing reusing
    manager->release(arr2);
    arr2 = manager->allocate("test2", 10);
    EXPECT_EQ(arr2, 15);

    manager->release(arr2);
    manager->release(q1);
    manager->release(q2);
    manager->release(q3);
    manager->release(q4);
    manager->release(q5);
    arr2 = manager->allocate("test2", 10);
    EXPECT_EQ(arr2, 15);

    manager->release(arr1);
    manager->release(arr2);
    arr2 = manager->allocate("test2", 10);
    EXPECT_EQ(arr2, 0);
}

TEST(AllocationManagerTestSuite, LinearAllocationTestNoReuse)
{
    auto manager = microsoft::quantum::BasicAllocationManager::createNew();
    manager->setReuseRegisters(false);

    auto q1 = manager->allocate();
    EXPECT_EQ(q1, 0);
    auto q2 = manager->allocate();
    EXPECT_EQ(q2, 1);
    auto q3 = manager->allocate();
    EXPECT_EQ(q3, 2);
    auto q4 = manager->allocate();
    EXPECT_EQ(q4, 3);
    auto q5 = manager->allocate();
    EXPECT_EQ(q5, 4);

    // We expect that allocating
    auto arr1 = manager->allocate("test", 10);
    EXPECT_EQ(arr1, 5);
    auto arr2 = manager->allocate("test2", 10);
    EXPECT_EQ(arr2, 15);

    // Testing reusing
    manager->release(arr2);
    arr2 = manager->allocate("test2", 10);
    EXPECT_EQ(arr2, 25);

    manager->release(arr2);
    manager->release(q1);
    manager->release(q2);
    manager->release(q3);
    manager->release(q4);
    manager->release(q5);
    arr2 = manager->allocate("test2", 10);
    EXPECT_EQ(arr2, 35);

    manager->release(arr1);
    manager->release(arr2);
    arr2 = manager->allocate("test2", 10);
    EXPECT_EQ(arr2, 45);
}

TEST(AllocationManagerTestSuite, InvalidRelease)
{
    auto manager = microsoft::quantum::BasicAllocationManager::createNew();
    auto q1      = manager->allocate();
    EXPECT_EQ(q1, 0);
    auto q2 = manager->allocate();
    EXPECT_EQ(q2, 1);

    EXPECT_THROW(manager->release(28837), std::runtime_error);
}
