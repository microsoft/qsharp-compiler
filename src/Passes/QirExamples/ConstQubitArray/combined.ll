; ModuleID = 'qat-link'
source_filename = "qat-link"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%Qubit = type opaque
%String = type opaque

declare %Qubit* @__quantum__rt__qubit_allocate() local_unnamed_addr

declare void @__quantum__rt__qubit_release(%Qubit*) local_unnamed_addr

declare void @__quantum__qis__h__body(%Qubit*) local_unnamed_addr

define void @ConstArrayReduction__Main() local_unnamed_addr #0 {
entry:
  tail call fastcc void @ConstArrayReduction__Main__body.11()
  %0 = tail call %String* @__quantum__rt__int_to_string(i64 0)
  ret void
}

declare %String* @__quantum__rt__int_to_string(i64) local_unnamed_addr

declare void @__quantum__rt__message(%String*) local_unnamed_addr

declare void @__quantum__rt__string_update_reference_count(%String*, i32) local_unnamed_addr

; Function Attrs: nofree
declare noalias nonnull i8* @_Znwm(i64) local_unnamed_addr #1

declare i32 @__gxx_personality_v0(...)

; Function Attrs: nofree
declare noalias nonnull i8* @_Znam(i64) local_unnamed_addr #1

declare i8* @__cxa_begin_catch(i8*) local_unnamed_addr

declare void @_ZSt9terminatev() local_unnamed_addr

declare void @_ZdlPv(i8*) local_unnamed_addr

declare void @_ZdaPv(i8*) local_unnamed_addr

declare void @llvm.memcpy.p0i8.p0i8.i64(i8*, i8*, i64, i1)

define internal fastcc void @ConstArrayReduction__Main__body.11() unnamed_addr personality i32 (...)* @__gxx_personality_v0 {
entry:
  %q1 = inttoptr i64 0 to %Qubit*
  %q2 = inttoptr i64 1 to %Qubit*
  %q3 = inttoptr i64 2 to %Qubit*
  %q4 = inttoptr i64 3 to %Qubit*
  %q5 = inttoptr i64 4 to %Qubit*
  %q6 = inttoptr i64 5 to %Qubit*
  %q7 = inttoptr i64 6 to %Qubit*
  %q8 = inttoptr i64 7 to %Qubit*
  %q9 = inttoptr i64 8 to %Qubit*
  %q10 = inttoptr i64 9 to %Qubit*
  %theQubit = inttoptr i64 10 to %Qubit*
  %0 = tail call noalias nonnull dereferenceable(40) i8* @_Znwm(i64 40) #2
  %1 = bitcast i8* %0 to i64*
  store i64 8, i64* %1, align 8
  %2 = getelementptr inbounds i8, i8* %0, i64 8
  %3 = bitcast i8* %2 to i64*
  store i64 10, i64* %3, align 8
  %4 = getelementptr inbounds i8, i8* %0, i64 24
  %5 = bitcast i8* %4 to i64*
  store i64 0, i64* %5, align 8
  %6 = getelementptr inbounds i8, i8* %0, i64 32
  %7 = bitcast i8* %6 to i64*
  store i64 1, i64* %7, align 8
  %8 = load i64, i64* %1, align 8
  %9 = load i64, i64* %3, align 8
  %10 = mul nsw i64 %9, %8
  %11 = call i8* @_Znam(i64 %10)
  br label %__quantum__rt__array_create_1d.exit

__quantum__rt__array_create_1d.exit:              ; preds = %entry
  %12 = getelementptr inbounds i8, i8* %0, i64 16
  %13 = bitcast i8* %12 to i8**
  store i8* %11, i8** %13, align 8
  %14 = bitcast i8* %11 to %Qubit**
  %15 = load i64, i64* %1, align 8
  %16 = getelementptr inbounds i8, i8* %11, i64 %15
  %17 = bitcast i8* %16 to %Qubit**
  %18 = shl nsw i64 %15, 1
  %19 = getelementptr inbounds i8, i8* %11, i64 %18
  %20 = bitcast i8* %19 to %Qubit**
  %21 = mul nsw i64 %15, 3
  %22 = getelementptr inbounds i8, i8* %11, i64 %21
  %23 = bitcast i8* %22 to %Qubit**
  %24 = shl nsw i64 %15, 2
  %25 = getelementptr inbounds i8, i8* %11, i64 %24
  %26 = bitcast i8* %25 to %Qubit**
  %27 = mul nsw i64 %15, 5
  %28 = getelementptr inbounds i8, i8* %11, i64 %27
  %29 = bitcast i8* %28 to %Qubit**
  %30 = mul nsw i64 %15, 6
  %31 = getelementptr inbounds i8, i8* %11, i64 %30
  %32 = bitcast i8* %31 to %Qubit**
  %33 = mul nsw i64 %15, 7
  %34 = getelementptr inbounds i8, i8* %11, i64 %33
  %35 = bitcast i8* %34 to %Qubit**
  %36 = shl nsw i64 %15, 3
  %37 = getelementptr inbounds i8, i8* %11, i64 %36
  %38 = bitcast i8* %37 to %Qubit**
  %39 = mul nsw i64 %15, 9
  %40 = getelementptr inbounds i8, i8* %11, i64 %39
  %41 = bitcast i8* %40 to %Qubit**
  store %Qubit* %q1, %Qubit** %14, align 8
  store %Qubit* %q2, %Qubit** %17, align 8
  store %Qubit* %q3, %Qubit** %20, align 8
  store %Qubit* %q4, %Qubit** %23, align 8
  store %Qubit* %q5, %Qubit** %26, align 8
  store %Qubit* %q6, %Qubit** %29, align 8
  store %Qubit* %q7, %Qubit** %32, align 8
  store %Qubit* %q8, %Qubit** %35, align 8
  store %Qubit* %q9, %Qubit** %38, align 8
  store %Qubit* %q10, %Qubit** %41, align 8
  %42 = load i64, i64* %5, align 8
  %43 = add nsw i64 %42, 1
  store i64 %43, i64* %5, align 8
  %44 = load i64, i64* %7, align 8
  %45 = add nsw i64 %44, 1
  store i64 %45, i64* %7, align 8
  %46 = load i64, i64* %5, align 8
  %47 = add nsw i64 %46, -1
  store i64 %47, i64* %5, align 8
  %48 = icmp sgt i64 %46, 1
  br i1 %48, label %49, label %__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge

__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge: ; preds = %__quantum__rt__array_create_1d.exit
  %.pre = load i8*, i8** %13, align 8
  br label %__quantum__rt__array_copy.exit

49:                                               ; preds = %__quantum__rt__array_create_1d.exit
  %50 = tail call noalias nonnull dereferenceable(40) i8* @_Znwm(i64 40) #2
  %51 = bitcast i8* %50 to i64*
  %52 = load i64, i64* %1, align 8
  store i64 %52, i64* %51, align 8
  %53 = getelementptr inbounds i8, i8* %50, i64 8
  %54 = bitcast i8* %53 to i64*
  %55 = load i64, i64* %3, align 8
  store i64 %55, i64* %54, align 8
  %56 = getelementptr inbounds i8, i8* %50, i64 24
  %57 = bitcast i8* %56 to i64*
  store i64 0, i64* %57, align 8
  %58 = getelementptr inbounds i8, i8* %50, i64 32
  %59 = bitcast i8* %58 to i64*
  store i64 1, i64* %59, align 8
  %60 = load i64, i64* %51, align 8
  %61 = load i64, i64* %54, align 8
  %62 = mul nsw i64 %61, %60
  %63 = call i8* @_Znam(i64 %62)
  br label %_ZN5ArrayC1EPS_.exit.i

_ZN5ArrayC1EPS_.exit.i:                           ; preds = %49
  %64 = getelementptr inbounds i8, i8* %50, i64 16
  %65 = bitcast i8* %64 to i8**
  store i8* %63, i8** %65, align 8
  %66 = load i8*, i8** %13, align 8
  %67 = load i64, i64* %51, align 8
  %68 = load i64, i64* %54, align 8
  %69 = mul nsw i64 %68, %67
  tail call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %66, i8* nonnull align 1 %63, i64 %69, i1 false) #3
  br label %__quantum__rt__array_copy.exit

__quantum__rt__array_copy.exit:                   ; preds = %_ZN5ArrayC1EPS_.exit.i, %__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge
  %.pre-phi7 = phi i64* [ %5, %__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge ], [ %57, %_ZN5ArrayC1EPS_.exit.i ]
  %.pre-phi6 = phi i64* [ %7, %__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge ], [ %59, %_ZN5ArrayC1EPS_.exit.i ]
  %.pre-phi5 = phi i64* [ %1, %__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge ], [ %51, %_ZN5ArrayC1EPS_.exit.i ]
  %.pre-phi = phi i8** [ %13, %__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge ], [ %65, %_ZN5ArrayC1EPS_.exit.i ]
  %70 = phi i8* [ %.pre, %__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge ], [ %63, %_ZN5ArrayC1EPS_.exit.i ]
  %.0.i.in = phi i8* [ %0, %__quantum__rt__array_create_1d.exit.__quantum__rt__array_copy.exit_crit_edge ], [ %50, %_ZN5ArrayC1EPS_.exit.i ]
  %71 = load i64, i64* %.pre-phi5, align 8
  %72 = mul nsw i64 %71, 7
  %73 = getelementptr inbounds i8, i8* %70, i64 %72
  %74 = bitcast i8* %73 to %Qubit**
  store %Qubit* %theQubit, %Qubit** %74, align 8
  %75 = load i64, i64* %.pre-phi6, align 8
  %76 = add nsw i64 %75, 1
  store i64 %76, i64* %.pre-phi6, align 8
  %77 = load i64, i64* %.pre-phi7, align 8
  %78 = icmp sgt i64 %77, 0
  br i1 %78, label %79, label %__quantum__rt__array_copy.exit.__quantum__rt__array_copy.exit3_crit_edge

__quantum__rt__array_copy.exit.__quantum__rt__array_copy.exit3_crit_edge: ; preds = %__quantum__rt__array_copy.exit
  %.pre4 = load i8*, i8** %.pre-phi, align 8
  br label %__quantum__rt__array_copy.exit3

79:                                               ; preds = %__quantum__rt__array_copy.exit
  %80 = tail call noalias nonnull dereferenceable(40) i8* @_Znwm(i64 40) #2
  %81 = bitcast i8* %80 to i64*
  %82 = load i64, i64* %.pre-phi5, align 8
  store i64 %82, i64* %81, align 8
  %83 = getelementptr inbounds i8, i8* %80, i64 8
  %84 = bitcast i8* %83 to i64*
  %85 = getelementptr inbounds i8, i8* %.0.i.in, i64 8
  %86 = bitcast i8* %85 to i64*
  %87 = load i64, i64* %86, align 8
  store i64 %87, i64* %84, align 8
  %88 = getelementptr inbounds i8, i8* %80, i64 24
  %89 = bitcast i8* %88 to i64*
  store i64 0, i64* %89, align 8
  %90 = getelementptr inbounds i8, i8* %80, i64 32
  %91 = bitcast i8* %90 to i64*
  store i64 1, i64* %91, align 8
  %92 = load i64, i64* %81, align 8
  %93 = load i64, i64* %84, align 8
  %94 = mul nsw i64 %93, %92
  %95 = call i8* @_Znam(i64 %94)
  br label %_ZN5ArrayC1EPS_.exit.i1

_ZN5ArrayC1EPS_.exit.i1:                          ; preds = %79
  %96 = getelementptr inbounds i8, i8* %80, i64 16
  %97 = bitcast i8* %96 to i8**
  store i8* %95, i8** %97, align 8
  %98 = load i8*, i8** %.pre-phi, align 8
  %99 = load i64, i64* %81, align 8
  %100 = load i64, i64* %84, align 8
  %101 = mul nsw i64 %100, %99
  tail call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %98, i8* nonnull align 1 %95, i64 %101, i1 false) #3
  br label %__quantum__rt__array_copy.exit3

__quantum__rt__array_copy.exit3:                  ; preds = %_ZN5ArrayC1EPS_.exit.i1, %__quantum__rt__array_copy.exit.__quantum__rt__array_copy.exit3_crit_edge
  %.pre-phi11 = phi i64* [ %.pre-phi7, %__quantum__rt__array_copy.exit.__quantum__rt__array_copy.exit3_crit_edge ], [ %89, %_ZN5ArrayC1EPS_.exit.i1 ]
  %.pre-phi10 = phi i64* [ %.pre-phi6, %__quantum__rt__array_copy.exit.__quantum__rt__array_copy.exit3_crit_edge ], [ %91, %_ZN5ArrayC1EPS_.exit.i1 ]
  %.pre-phi9 = phi i64* [ %.pre-phi5, %__quantum__rt__array_copy.exit.__quantum__rt__array_copy.exit3_crit_edge ], [ %81, %_ZN5ArrayC1EPS_.exit.i1 ]
  %.pre-phi8 = phi i8** [ %.pre-phi, %__quantum__rt__array_copy.exit.__quantum__rt__array_copy.exit3_crit_edge ], [ %97, %_ZN5ArrayC1EPS_.exit.i1 ]
  %102 = phi i8* [ %.pre4, %__quantum__rt__array_copy.exit.__quantum__rt__array_copy.exit3_crit_edge ], [ %95, %_ZN5ArrayC1EPS_.exit.i1 ]
  %103 = load i8*, i8** %.pre-phi, align 8
  %104 = load i64, i64* %.pre-phi5, align 8
  %105 = mul nsw i64 %104, 7
  %106 = getelementptr inbounds i8, i8* %103, i64 %105
  %107 = bitcast i8* %106 to i64*
  %108 = load i64, i64* %107, align 8
  %109 = load i64, i64* %.pre-phi9, align 8
  %110 = mul nsw i64 %109, 3
  %111 = getelementptr inbounds i8, i8* %102, i64 %110
  %112 = bitcast i8* %111 to i64*
  store i64 %108, i64* %112, align 8
  %113 = load i64, i64* %.pre-phi10, align 8
  %114 = add nsw i64 %113, 1
  store i64 %114, i64* %.pre-phi10, align 8
  %115 = load i64, i64* %.pre-phi11, align 8
  %116 = add nsw i64 %115, 1
  store i64 %116, i64* %.pre-phi11, align 8
  %117 = load i8*, i8** %.pre-phi8, align 8
  %118 = load i64, i64* %.pre-phi9, align 8
  %119 = mul nsw i64 %118, 3
  %120 = getelementptr inbounds i8, i8* %117, i64 %119
  %121 = bitcast i8* %120 to %Qubit**
  %122 = load %Qubit*, %Qubit** %121, align 8
  tail call void @__quantum__qis__h__body(%Qubit* %122)
  %123 = load i64, i64* %.pre-phi11, align 8
  %124 = add nsw i64 %123, -1
  store i64 %124, i64* %.pre-phi11, align 8
  %125 = load i64, i64* %7, align 8
  %126 = add nsw i64 %125, -2
  store i64 %126, i64* %7, align 8
  %127 = load i64, i64* %.pre-phi6, align 8
  %128 = add nsw i64 %127, -2
  store i64 %128, i64* %.pre-phi6, align 8
  %129 = load i64, i64* %.pre-phi10, align 8
  %130 = add nsw i64 %129, -2
  store i64 %130, i64* %.pre-phi10, align 8
  ret void
}

attributes #0 = { "EntryPoint" }
attributes #1 = { nofree }
attributes #2 = { builtin allocsize(0) }
attributes #3 = { nounwind }

!llvm.ident = !{!0}
!llvm.module.flags = !{!1, !2}

!0 = !{!"Homebrew clang version 11.1.0"}
!1 = !{i32 1, !"wchar_size", i32 4}
!2 = !{i32 7, !"PIC Level", i32 2}

