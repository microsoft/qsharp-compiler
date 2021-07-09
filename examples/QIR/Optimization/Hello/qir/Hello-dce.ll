; ModuleID = 'Hello.ll'
source_filename = "Hello.ll"

%String = type opaque

@0 = internal constant [21 x i8] c"Hello quantum world!\00"

define internal void @Hello__SayHello__body() {
entry:
  %0 = call %String* @__quantum__rt__string_create(i8* getelementptr inbounds ([21 x i8], [21 x i8]* @0, i64 0, i64 0))
  call void @__quantum__rt__message(%String* %0)
  call void @__quantum__rt__string_update_reference_count(%String* %0, i32 -1)
  ret void
}

declare %String* @__quantum__rt__string_create(i8*)

declare void @__quantum__rt__message(%String*)

declare void @__quantum__rt__string_update_reference_count(%String*, i32)

define void @Hello__SayHello__Interop() #0 {
entry:
  call void @Hello__HelloQ__body()
  ret void
}

define void @Hello__SayHello() #1 {
entry:
  call void @Hello__HelloQ__body()
  ret void
}

attributes #0 = { "InteropFriendly" }
attributes #1 = { "EntryPoint" }
