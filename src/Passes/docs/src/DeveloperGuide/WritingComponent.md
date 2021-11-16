# Tutorial: Writing a new component

In this tutorial we will develop a new QAT profile component. We will make the component a separate library which is dynamically loaded through the command line interface. All examples in this tutorial can be found in `ComponentExamples`.

Our first "component" will be a boilerplate hello world component which serves the purpose of giving the reader an understanding of how to define configurations for our component. We will demonstrate how to use this component from the command line.

For our second component, we will use a standard LLVM pass to demonstrate how to load these. We will show how the registered configuration can be used to enable or disable the pass. To show that the effect of the pass, we use the inliner pipeline together with the `QirExamples/LoopRecursion`. We will see how enabling the pass results in inlining all the function calls. As QAT ships with a built-in inliner pass, it is important to remember to disable this to see the effect of our custom pass.

## Hello world

Our first component will not do anything except for printing out a custom message upon configuring the profile. To this end, we need a configuration which allows the user to specify the message and we capture this configuration in a class which we name `HelloWorldConfig`:

```c++
using String = std::string;
class HelloWorldConfig
{
public:
  // ...
private:
  String message_{"Hello world"};
};
```

We note the default value of our configuration is captured through the initialisation of the class member. That is, if not overridden by the command line arguments, the message will be `"Hello world"`.

To fulfil the concept of being a configuration, a configuration must implement a `setup` function taking a reference to a `ConfigurationManager` as its only argument. For our configuration, this looks like

```c++
class HelloWorldConfig
{
public:
  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Hello world configuration",
                          "Demonstrating how configuration works.");
    config.addParameter(message_, "message",
                        "Message which is printed when setting the component up.");
  }

  // ...
};
```

The purpose of the `setup(config)` function is to inform the configuration manager about what the name of the configuration section and its description is as well as defining all settings and bind them to C++ variables. The benefit of this approach is that all configuration parameters for the component will be available immediately after the component is loaded by the tool.

The final code to manage the configuration reads:

```c++
class HelloWorldConfig
{
public:
  using String = std::string;

  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Hello world configuration",
                          "Demonstrating how configuration works.");
    config.addParameter(message_, "message",
                        "Message which is printed when setting the component up.");
  }

  String const& message() const
  {
    return message_;
  }

private:
  String message_{"Hello world"};
};
```

With the configuration in place, the next thing we concern ourselves with is loading the component. This is the functionality that registers the configuration together with an ID and a profile setup function. In our case, the setup function should just print a message given a `HelloWorldConfig` instance. The corresponding component registration reads:

```c++
extern "C" void loadComponent(IProfileGenerator *generator)
{
  generator->registerProfileComponent<HelloWorldConfig>(
      "hello-world",
      [](HelloWorldConfig const &cfg, IProfileGenerator * /*generator*/, Profile & /*profile*/) {
        std::cout << "Message: " << cfg.message() << std::endl;
      });
}
```

In this example, we will only concern ourselves with how to use the configuration and we will ignore `generator` and `profile` for now. The full source code to this example can be found in `ComponentExamples/HelloWorld` and it can be compiled through following steps (startig from the Passes root folder):

```sh
mkdir Debug
cd Debug
cmake ..
make HelloWorld
```

This will generate a `HelloWorld` dynamic library with path `./ComponentExamples/libHelloWorld.(dylib|so|dll)`.

## Loading the component

Executing `qat` and loading the `libHelloWorld` library, we see that our new settings are added to help page:

```sh
% ./qir/qat/Apps/qat --load ./ComponentExamples/libHelloWorld.dylib
Usage: ./qir/qat/Apps/qat [options] filename

...

Hello world configuration - Demonstration configuration for building a component boilerplate.

--message                                         Message which is printed when setting the component up. Default: Hello world


...
```

For the next part, we assume that you have a QIR located in `path/to/example.ll`. To test that the setup function is invoked upon setting the profile up, we run

```
 % ./qir/qat/Apps/qat --load ./ComponentExamples/libHelloWorld.dylib path/to/example.ll
Message: Hello world
```

## Creating a Pass Component

Next, we make a component that just runs a single LLVM pass. We we will use the inline pipeline to this end.

We create a single option for activating the pass:

```c++
class InlinerConfig
{
public:
  using String = std::string;

  void setup(ConfigurationManager &config)
  {
    config.setSectionName("Inliner component", "Adds the LLVM Always Inline Pass to the profile");
    config.addParameter(inline_, "custom-inliner", "Activating the custom inliner.");
  }

  bool shouldInline() const
  {
    return inline_;
  }

private:
  bool inline_{false};  ///< Default behaviour is that we do not add the inliner pass
};
```

The implementation itself is

```c++
extern "C" void loadComponent(IProfileGenerator *generator)
{
  generator->registerProfileComponent<InlinerConfig>(
      "inliner", [](InlinerConfig const &cfg, IProfileGenerator *ptr, Profile & /*profile*/) {
        if (cfg.shouldInline())
        {
          auto &module_pass_manager = ptr->modulePassManager();

          // Adds the inline pipeline
          auto &pass_builder = ptr->passBuilder();
          auto  inliner_pass = pass_builder.buildInlinerPipeline(
              ptr->optimisationLevel(), llvm::PassBuilder::ThinLTOPhase::None, ptr->debug());
          module_pass_manager.addPass(std::move(inliner_pass));
        }
      });
}

```

To run this pass,

```sh
./qir/qat/Apps/qat --load ./ComponentExamples/libInlinePassComponent.dylib ../QirExamples/LoopRecursion/QSharpVersion/qir/Example.ll --S --apply --no-always-inline --custom-inliner
```

Compare the output against

```sh
./qir/qat/Apps/qat --load ./ComponentExamples/libInlinePassComponent.dylib ../QirExamples/LoopRecursion/QSharpVersion/qir/Example.ll --S --apply --no-always-inline
```
