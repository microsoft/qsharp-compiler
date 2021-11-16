; ModuleID = 'test.ll'
source_filename = "test.ll"

define i32 @main() {
  %1 = alloca i32, align 4
  store i32 0, i32* %1, align 4
  call void (...) @__quatum__hello_world()
  ret i32 0
}

declare void @__quatum__hello_world(...)