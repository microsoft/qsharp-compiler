; ModuleID = 'combined.ll'
source_filename = "qat-link"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%Qubit = type opaque
%String = type opaque

declare void @__quantum__qis__h__body(%Qubit*) local_unnamed_addr

define void @ConstArrayReduction__Main() local_unnamed_addr #0 personality i32 (...)* @__gxx_personality_v0 {
ConstArrayReduction__Main__body.11.exit:
  tail call void @__quantum__qis__h__body(%Qubit* nonnull inttoptr (i64 10 to %Qubit*))
  %0 = tail call %String* @__quantum__rt__int_to_string(i64 0)
  ret void
}

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

declare i32 @__gxx_personality_v0(...)

attributes #0 = { "EntryPoint" }

!llvm.ident = !{!0}
!llvm.module.flags = !{!1, !2}

!0 = !{!"Homebrew clang version 11.1.0"}
!1 = !{i32 1, !"wchar_size", i32 4}
!2 = !{i32 7, !"PIC Level", i32 2}
