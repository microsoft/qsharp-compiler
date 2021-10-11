; ModuleID = 'qat-link'
source_filename = "qat-link"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%Tuple = type opaque
%String = type opaque

define void @ConstArrayReduction__Main() local_unnamed_addr #0 {
entry:
  %0 = tail call noalias nonnull dereferenceable(40) i8* @_Znam(i64 40) #2
  %1 = bitcast i8* %0 to i64*
  %2 = getelementptr inbounds i8, i8* %0, i64 8
  %3 = bitcast i8* %2 to i64*
  %4 = getelementptr inbounds i8, i8* %0, i64 16
  %5 = bitcast i8* %4 to i64*
  store i64 16, i64* %1, align 8, !tbaa !3
  store i64 1, i64* %3, align 8, !tbaa !3
  store i64 0, i64* %5, align 8, !tbaa !3
  %6 = getelementptr inbounds i8, i8* %0, i64 24
  %7 = bitcast i8* %6 to %Tuple*
  %8 = bitcast %Tuple* %7 to { i64, i64 }*
  %9 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %8, i64 0, i32 0
  %10 = getelementptr inbounds { i64, i64 }, { i64, i64 }* %8, i64 0, i32 1
  store i64 5, i64* %9, align 4
  store i64 3, i64* %10, align 4
  %x.i = load i64, i64* %9, align 4
  %11 = load i64, i64* %3, align 8, !tbaa !3
  %12 = add nsw i64 %11, -1
  store i64 %12, i64* %3, align 8, !tbaa !3
  %13 = icmp slt i64 %11, 2
  br i1 %13, label %14, label %ConstArrayReduction__Main__body.exit

14:                                               ; preds = %entry
  tail call void @_ZdlPv(i8* nonnull %0) #3
  br label %ConstArrayReduction__Main__body.exit

ConstArrayReduction__Main__body.exit:             ; preds = %entry, %14
  %15 = mul i64 %x.i, 3
  %16 = tail call %String* @__quantum__rt__int_to_string(i64 %15)
  ret void
}

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

; Function Attrs: nofree
declare noalias nonnull i8* @_Znam(i64) local_unnamed_addr #1

declare void @_ZdlPv(i8*) local_unnamed_addr

attributes #0 = { "EntryPoint" "requiredQubits"="0" }
attributes #1 = { nofree }
attributes #2 = { builtin allocsize(0) }
attributes #3 = { builtin nounwind }

!llvm.ident = !{!0}
!llvm.module.flags = !{!1, !2}

!0 = !{!"Homebrew clang version 11.1.0"}
!1 = !{i32 1, !"wchar_size", i32 4}
!2 = !{i32 7, !"PIC Level", i32 2}
!3 = !{!4, !4, i64 0}
!4 = !{!"long long", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}

