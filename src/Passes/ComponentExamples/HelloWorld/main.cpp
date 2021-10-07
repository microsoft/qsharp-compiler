// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#include "Commandline/ConfigurationManager.hpp"
#include "Generators/IProfileGenerator.hpp"
using namespace microsoft::quantum;

extern "C" void loadComponent(IProfileGenerator *generator);

class HelloWorldConfig
{
public:
  using String = std::string;

  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Hello world configuration",
                          "Demonstration configuration for building a component boilerplate.");
    config.addParameter(message_, "message",
                        "Message which is printed when setting the component up.");
  }

  String message() const
  {
    return message_;
  }

private:
  String message_{"Hello world"};
};

extern "C" void loadComponent(IProfileGenerator *generator)
{
  generator->registerProfileComponent<HelloWorldConfig>(
      "hello-world",
      [](HelloWorldConfig const &cfg, IProfileGenerator * /*ptr*/, Profile & /*profile*/) {
        std::cout << "Message: " << cfg.message() << std::endl;
      });
}
