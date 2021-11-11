# Developer FAQ

## Pass does not load

One error that you may encounter is that an analysis pass does not load with output similar to this:

```sh
opt -load-pass-plugin ../../Debug/libQSharpPasses.dylib -enable-debugify  --passes="operation-counter" -disable-output   classical-program.bc
Failed to load passes from '../../Debug/libQSharpPasses.dylib'. Request ignored.
opt: unknown pass name 'operation-counter'
```

This is likely becuase you have forgotten to instantiate static class members. For instance, in the case of an instance of `llvm::AnalysisInfoMixin` you are required to have a static member `Key`:

```cpp
class COpsCounterPass :  public llvm::AnalysisInfoMixin<COpsCounterPass> {
private:
  static llvm::AnalysisKey Key; //< REQUIRED by llvm registration
  friend struct llvm::AnalysisInfoMixin<COpsCounterPass>;
};
```

If you forget to instantiate this variable in your corresponding `.cpp` file,

```cpp
// llvm::AnalysisKey COpsCounterPass::Key; //< Uncomment this line to make everything work
```

everything will compile, but the pass will fail to load. There will be no linking errors either.
