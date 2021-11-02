// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "AllocationManager/AllocationManager.hpp"
#include "Commandline/ConfigurationManager.hpp"
#include "gtest/gtest.h"

using namespace microsoft::quantum;

namespace {

class TestConfig1
{
public:
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Base configuration", "");
    config.addParameter(param1_, "param1", "");
    config.addParameter(param2_, "param2", "");
    config.addParameter(param3_, "param3", "");
  }

  bool param1() const
  {
    return param1_;
  }

  std::string param2() const
  {
    return param2_;
  }

  int32_t param3() const
  {
    return param3_;
  }

private:
  bool        param1_{false};
  std::string param2_{""};
  int32_t     param3_{9};
};

class TestConfig2
{
public:
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Base configuration", "");
    config.addParameter(param1_, "param1", "");
    config.addParameter(param2_, "param2", "");
    config.addParameter(param3_, "param3", "");
  }

  bool param1() const
  {
    return param1_;
  }

  std::string param2() const
  {
    return param2_;
  }

  int32_t param3() const
  {
    return param3_;
  }

private:
  bool        param1_{true};
  std::string param2_{"xxxx"};
  int32_t     param3_{9};
};

}  // namespace

TEST(CommandlineTestSuite, Configuration)
{
  {
    ConfigurationManager configuration_manager;
    configuration_manager.addConfig<TestConfig1>();

    ParameterParser parser;
    configuration_manager.setupArguments(parser);
    char *args[] = {"main", "--param1", "--param2", "hello", "--param3", "1337"};
    parser.parseArgs(6, args);

    configuration_manager.configure(parser);

    auto &config = configuration_manager.get<TestConfig1>();
    EXPECT_EQ(config.param1(), true);
    EXPECT_EQ(config.param2(), "hello");
    EXPECT_EQ(config.param3(), 1337);
  }

  {
    ConfigurationManager configuration_manager;
    configuration_manager.addConfig<TestConfig1>();

    ParameterParser parser;
    configuration_manager.setupArguments(parser);
    char *args[] = {"main", "--no-param1", "--param2", "ms", "--param3", "17372"};
    parser.parseArgs(6, args);

    configuration_manager.configure(parser);

    auto &config = configuration_manager.get<TestConfig1>();
    EXPECT_EQ(config.param1(), false);
    EXPECT_EQ(config.param2(), "ms");
    EXPECT_EQ(config.param3(), 17372);
  }

  // Testing default values
  {
    // Testing default arguments
    ConfigurationManager configuration_manager;
    configuration_manager.addConfig<TestConfig2>();

    ParameterParser parser;
    configuration_manager.setupArguments(parser);
    char *args[] = {"main"};
    parser.parseArgs(1, args);

    configuration_manager.configure(parser);

    auto &config = configuration_manager.get<TestConfig2>();
    EXPECT_EQ(config.param1(), true);
    EXPECT_EQ(config.param2(), "xxxx");
    EXPECT_EQ(config.param3(), 9);
  }

  // Testing opposite boolean default
  {
    ConfigurationManager configuration_manager;
    configuration_manager.addConfig<TestConfig2>();

    ParameterParser parser;
    configuration_manager.setupArguments(parser);
    char *args[] = {"main", "--no-param1", "--param2", "msss", "--param3", "17372"};
    parser.parseArgs(6, args);

    configuration_manager.configure(parser);

    auto &config = configuration_manager.get<TestConfig2>();
    EXPECT_EQ(config.param1(), false);
    EXPECT_EQ(config.param2(), "msss");
    EXPECT_EQ(config.param3(), 17372);
  }
}
