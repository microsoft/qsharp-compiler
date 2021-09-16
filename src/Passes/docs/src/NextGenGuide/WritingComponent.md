# Tutorial: Writing a new component

## Goal

- What should we have archived at the end of this tutorial

## Getting started

- Defining the transformation we wish
- Creating a rule based path for the transformation
- Adding it as a component to the profile

## Creating the boiler plate

```c++
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
```

```c++
extern "C" void loadComponent(IProfileGenerator *generator)
{
  generator->registerProfileComponent<HelloWorldConfig>(
      "hello-world",
      [](HelloWorldConfig const &cfg, IProfileGenerator * /*ptr*/, Profile & /*profile*/) {
        std::cout << "Message: " << cfg.message() << std::endl;
      });
}
```

## Loading the component

```sh
% ./Source/Apps/qat --load ./ComponentExamples/libHelloWorld.dylib
Usage: ./Source/Apps/qat [options] filename

...

Hello world configuration - Demonstration configuration for building a component boilerplate.

--message                                         Message which is printed when setting the component up. Default: Hello world


...
```

Next we generate a QIR to test this with:

```
pushd ../QirExamples/LoopRecursion/QSharpVersion
make
popd
```

To test that the setup function is invoked upon generating the profile, we run

```
 % ./Source/Apps/qat --load ./ComponentExamples/libHelloWorld.dylib ../QirExamples/LoopRecursion/QSharpVersion/qir/Example.ll
Message: Hello world
```

```sh
% ./Source/Apps/qat --load ./ComponentExamples/libHelloWorld.dylib
```

## Creating a Pass Component

Next, we make a component that just runs a single LLVM pass. We we will use the inline pipeline to this end.

We create a single option for activating the pass:

```sh
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
./Source/Apps/qat --load ./ComponentExamples/libInlinePassComponent.dylib ../QirExamples/LoopRecursion/QSharpVersion/qir/Example.ll --S --apply --no-always-inline --custom-inliner
```

Compare the output against

```sh
./Source/Apps/qat --load ./ComponentExamples/libInlinePassComponent.dylib ../QirExamples/LoopRecursion/QSharpVersion/qir/Example.ll --S --apply --no-always-inline
```
