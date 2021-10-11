; ModuleID = 'combined.ll'
source_filename = "qat-link"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%String = type opaque

define void @ConstArrayReduction__Main() local_unnamed_addr #0 {
ConstArrayReduction__Main__body.exit:
  %0 = tail call %String* @__quantum__rt__int_to_string(i64 15)
  ret void
}

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

attributes #0 = { "EntryPoint" "requiredQubits"="0" }

!llvm.ident = !{!0}
!llvm.module.flags = !{!1, !2}

!0 = !{!"Homebrew clang version 11.1.0"}
!1 = !{i32 1, !"wchar_size", i32 4}
!2 = !{i32 7, !"PIC Level", i32 2}
