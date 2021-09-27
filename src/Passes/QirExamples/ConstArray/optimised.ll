; ModuleID = 'combined.ll'
source_filename = "llvm-link"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%Array = type { i64, i64, i8*, i64, i64 }

; Function Attrs: norecurse nounwind readnone
define i64 @ConstArrayReduction__Main__Interop() local_unnamed_addr #0 personality i32 (...)* @__gxx_personality_v0 {
entry:
  ret i64 1337
}

; Function Attrs: noreturn nounwind
define void @ConstArrayReduction__Main() local_unnamed_addr #1 {
entry:
  tail call void @llvm.trap()
  unreachable
}

define noalias nonnull %Array* @__quantum__rt__array_create_1d(i32 %0, i64 %1) local_unnamed_addr personality i32 (...)* @__gxx_personality_v0 {
  %3 = tail call noalias nonnull dereferenceable(40) i8* @_Znwm(i64 40)
  %4 = sext i32 %0 to i64
  %5 = bitcast i8* %3 to i64*
  store i64 %4, i64* %5, align 8
  %6 = getelementptr inbounds i8, i8* %3, i64 8
  %7 = bitcast i8* %6 to i64*
  store i64 %1, i64* %7, align 8
  %8 = getelementptr inbounds i8, i8* %3, i64 24
  %9 = bitcast i8* %8 to <2 x i64>*
  store <2 x i64> <i64 0, i64 1>, <2 x i64>* %9, align 8
  %10 = mul nsw i64 %4, %1
  %11 = invoke noalias nonnull i8* @_Znam(i64 %10)
          to label %_ZN5ArrayC1Exx.exit unwind label %12

12:                                               ; preds = %2
  %13 = landingpad { i8*, i32 }
          catch i8* null
  %14 = extractvalue { i8*, i32 } %13, 0
  tail call void @__clang_call_terminate(i8* %14)
  unreachable

_ZN5ArrayC1Exx.exit:                              ; preds = %2
  %15 = bitcast i8* %3 to %Array*
  %16 = getelementptr inbounds i8, i8* %3, i64 16
  %17 = bitcast i8* %16 to i8**
  store i8* %11, i8** %17, align 8
  ret %Array* %15
}

; Function Attrs: nofree
declare noalias nonnull i8* @_Znwm(i64) local_unnamed_addr #2

declare i32 @__gxx_personality_v0(...)

; Function Attrs: nofree
declare noalias nonnull i8* @_Znam(i64) local_unnamed_addr #2

define linkonce_odr hidden void @__clang_call_terminate(i8* %0) local_unnamed_addr {
  %2 = tail call i8* @__cxa_begin_catch(i8* %0)
  tail call void @_ZSt9terminatev()
  unreachable
}

declare i8* @__cxa_begin_catch(i8*) local_unnamed_addr

declare void @_ZSt9terminatev() local_unnamed_addr

; Function Attrs: norecurse nounwind readonly
define i8* @__quantum__rt__array_get_element_ptr_1d(%Array* nocapture readonly %0, i64 %1) local_unnamed_addr #3 {
  %3 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 2
  %4 = load i8*, i8** %3, align 8
  %5 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 0
  %6 = load i64, i64* %5, align 8
  %7 = mul nsw i64 %6, %1
  %8 = getelementptr inbounds i8, i8* %4, i64 %7
  ret i8* %8
}

define void @__quantum__rt__qubit_release_array(%Array* %0) local_unnamed_addr {
  %2 = icmp eq %Array* %0, null
  br i1 %2, label %9, label %3

3:                                                ; preds = %1
  %4 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 2
  %5 = load i8*, i8** %4, align 8
  %6 = icmp eq i8* %5, null
  br i1 %6, label %_ZN5ArrayD1Ev.exit, label %7

7:                                                ; preds = %3
  tail call void @_ZdaPv(i8* nonnull %5)
  br label %_ZN5ArrayD1Ev.exit

_ZN5ArrayD1Ev.exit:                               ; preds = %7, %3
  %8 = bitcast %Array* %0 to i8*
  tail call void @_ZdlPv(i8* %8)
  br label %9

9:                                                ; preds = %_ZN5ArrayD1Ev.exit, %1
  ret void
}

declare void @_ZdlPv(i8*) local_unnamed_addr

declare void @_ZdaPv(i8*) local_unnamed_addr

; Function Attrs: nofree norecurse nounwind
define void @__quantum__rt__array_update_alias_count(%Array* nocapture %0, i32 %1) local_unnamed_addr #4 {
  %3 = sext i32 %1 to i64
  %4 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 3
  %5 = load i64, i64* %4, align 8
  %6 = add nsw i64 %5, %3
  store i64 %6, i64* %4, align 8
  ret void
}

; Function Attrs: nofree norecurse nounwind
define void @__quantum__rt__array_update_reference_count(%Array* nocapture %0, i32 %1) local_unnamed_addr #4 {
  %3 = sext i32 %1 to i64
  %4 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 4
  %5 = load i64, i64* %4, align 8
  %6 = add nsw i64 %5, %3
  store i64 %6, i64* %4, align 8
  ret void
}

define %Array* @__quantum__rt__array_copy(%Array* readonly %0, i1 zeroext %1) local_unnamed_addr personality i32 (...)* @__gxx_personality_v0 {
  %3 = icmp eq %Array* %0, null
  br i1 %3, label %30, label %4

4:                                                ; preds = %2
  br i1 %1, label %9, label %5

5:                                                ; preds = %4
  %6 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 3
  %7 = load i64, i64* %6, align 8
  %8 = icmp sgt i64 %7, 0
  br i1 %8, label %9, label %30

9:                                                ; preds = %5, %4
  %10 = tail call noalias nonnull dereferenceable(40) i8* @_Znwm(i64 40)
  %11 = bitcast i8* %10 to i64*
  %12 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 0
  %13 = load i64, i64* %12, align 8
  store i64 %13, i64* %11, align 8
  %14 = getelementptr inbounds i8, i8* %10, i64 8
  %15 = bitcast i8* %14 to i64*
  %16 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 1
  %17 = load i64, i64* %16, align 8
  store i64 %17, i64* %15, align 8
  %18 = getelementptr inbounds i8, i8* %10, i64 24
  %19 = bitcast i8* %18 to <2 x i64>*
  store <2 x i64> <i64 0, i64 1>, <2 x i64>* %19, align 8
  %20 = mul nsw i64 %17, %13
  %21 = invoke noalias nonnull i8* @_Znam(i64 %20)
          to label %_ZN5ArrayC1EPS_.exit unwind label %22

22:                                               ; preds = %9
  %23 = landingpad { i8*, i32 }
          catch i8* null
  %24 = extractvalue { i8*, i32 } %23, 0
  tail call void @__clang_call_terminate(i8* %24)
  unreachable

_ZN5ArrayC1EPS_.exit:                             ; preds = %9
  %25 = bitcast i8* %10 to %Array*
  %26 = getelementptr inbounds i8, i8* %10, i64 16
  %27 = bitcast i8* %26 to i8**
  store i8* %21, i8** %27, align 8
  %28 = getelementptr inbounds %Array, %Array* %0, i64 0, i32 2
  %29 = load i8*, i8** %28, align 8
  tail call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %29, i8* nonnull align 1 %21, i64 %20, i1 false)
  br label %30

30:                                               ; preds = %_ZN5ArrayC1EPS_.exit, %5, %2
  %.0 = phi %Array* [ %25, %_ZN5ArrayC1EPS_.exit ], [ null, %2 ], [ %0, %5 ]
  ret %Array* %.0
}

; Function Attrs: argmemonly nounwind willreturn
declare void @llvm.memcpy.p0i8.p0i8.i64(i8* noalias nocapture writeonly, i8* noalias nocapture readonly, i64, i1 immarg) #5

; Function Attrs: cold noreturn nounwind
declare void @llvm.trap() #6

attributes #0 = { norecurse nounwind readnone "InteropFriendly" }
attributes #1 = { noreturn nounwind "EntryPoint" }
attributes #2 = { nofree }
attributes #3 = { norecurse nounwind readonly }
attributes #4 = { nofree norecurse nounwind }
attributes #5 = { argmemonly nounwind willreturn }
attributes #6 = { cold noreturn nounwind }

!llvm.ident = !{!0}
!llvm.module.flags = !{!1, !2}

!0 = !{!"Homebrew clang version 11.1.0"}
!1 = !{i32 1, !"wchar_size", i32 4}
!2 = !{i32 7, !"PIC Level", i32 2}
