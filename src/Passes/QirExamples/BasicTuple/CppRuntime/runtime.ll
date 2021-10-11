; ModuleID = 'runtime.cpp'
source_filename = "runtime.cpp"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%class.Array = type { i64, i64, i8*, i64, i64 }

; Function Attrs: ssp uwtable
define i8* @__quantum__rt__tuple_create(i64 %0) #0 {
  %2 = alloca i64, align 8
  %3 = alloca i8*, align 8
  %4 = alloca i64*, align 8
  %5 = alloca i64*, align 8
  %6 = alloca i64*, align 8
  store i64 %0, i64* %2, align 8, !tbaa !3
  %7 = bitcast i8** %3 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %7) #7
  %8 = load i64, i64* %2, align 8, !tbaa !3
  %9 = add i64 %8, 24
  %10 = call noalias nonnull i8* @_Znam(i64 %9) #8
  store i8* %10, i8** %3, align 8, !tbaa !7
  %11 = bitcast i64** %4 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %11) #7
  %12 = load i8*, i8** %3, align 8, !tbaa !7
  %13 = bitcast i8* %12 to i64*
  store i64* %13, i64** %4, align 8, !tbaa !7
  %14 = bitcast i64** %5 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %14) #7
  %15 = load i8*, i8** %3, align 8, !tbaa !7
  %16 = getelementptr inbounds i8, i8* %15, i64 8
  %17 = bitcast i8* %16 to i64*
  store i64* %17, i64** %5, align 8, !tbaa !7
  %18 = bitcast i64** %6 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %18) #7
  %19 = load i8*, i8** %3, align 8, !tbaa !7
  %20 = getelementptr inbounds i8, i8* %19, i64 16
  %21 = bitcast i8* %20 to i64*
  store i64* %21, i64** %6, align 8, !tbaa !7
  %22 = load i64, i64* %2, align 8, !tbaa !3
  %23 = load i64*, i64** %4, align 8, !tbaa !7
  store i64 %22, i64* %23, align 8, !tbaa !3
  %24 = load i64*, i64** %5, align 8, !tbaa !7
  store i64 1, i64* %24, align 8, !tbaa !3
  %25 = load i64*, i64** %6, align 8, !tbaa !7
  store i64 0, i64* %25, align 8, !tbaa !3
  %26 = load i8*, i8** %3, align 8, !tbaa !7
  %27 = getelementptr inbounds i8, i8* %26, i64 24
  %28 = bitcast i64** %6 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %28) #7
  %29 = bitcast i64** %5 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %29) #7
  %30 = bitcast i64** %4 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %30) #7
  %31 = bitcast i8** %3 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %31) #7
  ret i8* %27
}

; Function Attrs: argmemonly nounwind willreturn
declare void @llvm.lifetime.start.p0i8(i64 immarg, i8* nocapture) #1

; Function Attrs: nobuiltin allocsize(0)
declare nonnull i8* @_Znam(i64) #2

; Function Attrs: argmemonly nounwind willreturn
declare void @llvm.lifetime.end.p0i8(i64 immarg, i8* nocapture) #1

; Function Attrs: nounwind ssp uwtable
define void @__quantum__rt__tuple_update_reference_count(i8* %0, i32 %1) #3 {
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i8*, align 8
  %6 = alloca i64*, align 8
  store i8* %0, i8** %3, align 8, !tbaa !7
  store i32 %1, i32* %4, align 4, !tbaa !9
  %7 = bitcast i8** %5 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %7) #7
  %8 = load i8*, i8** %3, align 8, !tbaa !7
  %9 = getelementptr inbounds i8, i8* %8, i64 -24
  store i8* %9, i8** %5, align 8, !tbaa !7
  %10 = bitcast i64** %6 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %10) #7
  %11 = load i8*, i8** %5, align 8, !tbaa !7
  %12 = getelementptr inbounds i8, i8* %11, i64 8
  %13 = bitcast i8* %12 to i64*
  store i64* %13, i64** %6, align 8, !tbaa !7
  %14 = load i32, i32* %4, align 4, !tbaa !9
  %15 = sext i32 %14 to i64
  %16 = load i64*, i64** %6, align 8, !tbaa !7
  %17 = load i64, i64* %16, align 8, !tbaa !3
  %18 = add nsw i64 %17, %15
  store i64 %18, i64* %16, align 8, !tbaa !3
  %19 = load i64*, i64** %6, align 8, !tbaa !7
  %20 = load i64, i64* %19, align 8, !tbaa !3
  %21 = icmp sle i64 %20, 0
  br i1 %21, label %22, label %27

22:                                               ; preds = %2
  %23 = load i8*, i8** %5, align 8, !tbaa !7
  %24 = icmp eq i8* %23, null
  br i1 %24, label %26, label %25

25:                                               ; preds = %22
  call void @_ZdlPv(i8* %23) #9
  br label %26

26:                                               ; preds = %25, %22
  br label %27

27:                                               ; preds = %26, %2
  %28 = bitcast i64** %6 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %28) #7
  %29 = bitcast i8** %5 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %29) #7
  ret void
}

; Function Attrs: nobuiltin nounwind
declare void @_ZdlPv(i8*) #4

; Function Attrs: ssp uwtable
define %class.Array* @__quantum__rt__array_create_1d(i32 %0, i64 %1) #0 {
  %3 = alloca i32, align 4
  %4 = alloca i64, align 8
  store i32 %0, i32* %3, align 4, !tbaa !9
  store i64 %1, i64* %4, align 8, !tbaa !3
  %5 = call noalias nonnull i8* @_Znwm(i64 40) #8
  %6 = bitcast i8* %5 to %class.Array*
  %7 = load i32, i32* %3, align 4, !tbaa !9
  %8 = sext i32 %7 to i64
  %9 = load i64, i64* %4, align 8, !tbaa !3
  call void @_ZN5ArrayC1Exx(%class.Array* %6, i64 %8, i64 %9) #7
  ret %class.Array* %6
}

; Function Attrs: nobuiltin allocsize(0)
declare nonnull i8* @_Znwm(i64) #2

; Function Attrs: nounwind ssp uwtable
define linkonce_odr void @_ZN5ArrayC1Exx(%class.Array* %0, i64 %1, i64 %2) unnamed_addr #3 align 2 {
  %4 = alloca %class.Array*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  store %class.Array* %0, %class.Array** %4, align 8, !tbaa !7
  store i64 %1, i64* %5, align 8, !tbaa !3
  store i64 %2, i64* %6, align 8, !tbaa !3
  %7 = load %class.Array*, %class.Array** %4, align 8
  %8 = load i64, i64* %5, align 8, !tbaa !3
  %9 = load i64, i64* %6, align 8, !tbaa !3
  call void @_ZN5ArrayC2Exx(%class.Array* %7, i64 %8, i64 %9) #7
  ret void
}

; Function Attrs: nounwind ssp uwtable
define i8* @__quantum__rt__array_get_element_ptr_1d(%class.Array* %0, i64 %1) #3 {
  %3 = alloca %class.Array*, align 8
  %4 = alloca i64, align 8
  store %class.Array* %0, %class.Array** %3, align 8, !tbaa !7
  store i64 %1, i64* %4, align 8, !tbaa !3
  %5 = load %class.Array*, %class.Array** %3, align 8, !tbaa !7
  %6 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 2
  %7 = load i8*, i8** %6, align 8, !tbaa !11
  %8 = load i64, i64* %4, align 8, !tbaa !3
  %9 = load %class.Array*, %class.Array** %3, align 8, !tbaa !7
  %10 = getelementptr inbounds %class.Array, %class.Array* %9, i32 0, i32 0
  %11 = load i64, i64* %10, align 8, !tbaa !13
  %12 = mul nsw i64 %8, %11
  %13 = getelementptr inbounds i8, i8* %7, i64 %12
  ret i8* %13
}

; Function Attrs: nounwind ssp uwtable
define void @__quantum__rt__qubit_release_array(%class.Array* %0) #3 {
  %2 = alloca %class.Array*, align 8
  store %class.Array* %0, %class.Array** %2, align 8, !tbaa !7
  %3 = load %class.Array*, %class.Array** %2, align 8, !tbaa !7
  %4 = icmp eq %class.Array* %3, null
  br i1 %4, label %7, label %5

5:                                                ; preds = %1
  call void @_ZN5ArrayD1Ev(%class.Array* %3) #7
  %6 = bitcast %class.Array* %3 to i8*
  call void @_ZdlPv(i8* %6) #9
  br label %7

7:                                                ; preds = %5, %1
  ret void
}

; Function Attrs: nounwind ssp uwtable
define linkonce_odr void @_ZN5ArrayD1Ev(%class.Array* %0) unnamed_addr #3 align 2 {
  %2 = alloca %class.Array*, align 8
  store %class.Array* %0, %class.Array** %2, align 8, !tbaa !7
  %3 = load %class.Array*, %class.Array** %2, align 8
  call void @_ZN5ArrayD2Ev(%class.Array* %3) #7
  ret void
}

; Function Attrs: nounwind ssp uwtable
define void @__quantum__rt__array_update_alias_count(%class.Array* %0, i32 %1) #3 {
  %3 = alloca %class.Array*, align 8
  %4 = alloca i32, align 4
  store %class.Array* %0, %class.Array** %3, align 8, !tbaa !7
  store i32 %1, i32* %4, align 4, !tbaa !9
  %5 = load i32, i32* %4, align 4, !tbaa !9
  %6 = sext i32 %5 to i64
  %7 = load %class.Array*, %class.Array** %3, align 8, !tbaa !7
  %8 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 3
  %9 = load i64, i64* %8, align 8, !tbaa !14
  %10 = add nsw i64 %9, %6
  store i64 %10, i64* %8, align 8, !tbaa !14
  ret void
}

; Function Attrs: nounwind ssp uwtable
define void @__quantum__rt__array_update_reference_count(%class.Array* %0, i32 %1) #3 {
  %3 = alloca %class.Array*, align 8
  %4 = alloca i32, align 4
  store %class.Array* %0, %class.Array** %3, align 8, !tbaa !7
  store i32 %1, i32* %4, align 4, !tbaa !9
  %5 = load i32, i32* %4, align 4, !tbaa !9
  %6 = sext i32 %5 to i64
  %7 = load %class.Array*, %class.Array** %3, align 8, !tbaa !7
  %8 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 4
  %9 = load i64, i64* %8, align 8, !tbaa !15
  %10 = add nsw i64 %9, %6
  store i64 %10, i64* %8, align 8, !tbaa !15
  ret void
}

; Function Attrs: ssp uwtable
define %class.Array* @__quantum__rt__array_copy(%class.Array* %0, i1 zeroext %1) #0 {
  %3 = alloca %class.Array*, align 8
  %4 = alloca %class.Array*, align 8
  %5 = alloca i8, align 1
  store %class.Array* %0, %class.Array** %4, align 8, !tbaa !7
  %6 = zext i1 %1 to i8
  store i8 %6, i8* %5, align 1, !tbaa !16
  %7 = load %class.Array*, %class.Array** %4, align 8, !tbaa !7
  %8 = icmp eq %class.Array* %7, null
  br i1 %8, label %9, label %10

9:                                                ; preds = %2
  store %class.Array* null, %class.Array** %3, align 8
  br label %24

10:                                               ; preds = %2
  %11 = load i8, i8* %5, align 1, !tbaa !16, !range !18
  %12 = trunc i8 %11 to i1
  br i1 %12, label %18, label %13

13:                                               ; preds = %10
  %14 = load %class.Array*, %class.Array** %4, align 8, !tbaa !7
  %15 = getelementptr inbounds %class.Array, %class.Array* %14, i32 0, i32 3
  %16 = load i64, i64* %15, align 8, !tbaa !14
  %17 = icmp sgt i64 %16, 0
  br i1 %17, label %18, label %22

18:                                               ; preds = %13, %10
  %19 = call noalias nonnull i8* @_Znwm(i64 40) #8
  %20 = bitcast i8* %19 to %class.Array*
  %21 = load %class.Array*, %class.Array** %4, align 8, !tbaa !7
  call void @_ZN5ArrayC1EPS_(%class.Array* %20, %class.Array* %21) #7
  store %class.Array* %20, %class.Array** %3, align 8
  br label %24

22:                                               ; preds = %13
  %23 = load %class.Array*, %class.Array** %4, align 8, !tbaa !7
  store %class.Array* %23, %class.Array** %3, align 8
  br label %24

24:                                               ; preds = %22, %18, %9
  %25 = load %class.Array*, %class.Array** %3, align 8
  ret %class.Array* %25
}

; Function Attrs: nounwind ssp uwtable
define linkonce_odr void @_ZN5ArrayC1EPS_(%class.Array* %0, %class.Array* %1) unnamed_addr #3 align 2 {
  %3 = alloca %class.Array*, align 8
  %4 = alloca %class.Array*, align 8
  store %class.Array* %0, %class.Array** %3, align 8, !tbaa !7
  store %class.Array* %1, %class.Array** %4, align 8, !tbaa !7
  %5 = load %class.Array*, %class.Array** %3, align 8
  %6 = load %class.Array*, %class.Array** %4, align 8, !tbaa !7
  call void @_ZN5ArrayC2EPS_(%class.Array* %5, %class.Array* %6) #7
  ret void
}

; Function Attrs: nounwind ssp uwtable
define linkonce_odr void @_ZN5ArrayC2Exx(%class.Array* %0, i64 %1, i64 %2) unnamed_addr #3 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %4 = alloca %class.Array*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  store %class.Array* %0, %class.Array** %4, align 8, !tbaa !7
  store i64 %1, i64* %5, align 8, !tbaa !3
  store i64 %2, i64* %6, align 8, !tbaa !3
  %7 = load %class.Array*, %class.Array** %4, align 8
  %8 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 0
  %9 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__14moveIRxEEONS_16remove_referenceIT_E4typeEOS3_(i64* nonnull align 8 dereferenceable(8) %5) #7
  %10 = load i64, i64* %9, align 8, !tbaa !3
  store i64 %10, i64* %8, align 8, !tbaa !13
  %11 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 1
  %12 = call nonnull align 8 dereferenceable(8) i64* @_ZNSt3__14moveIRxEEONS_16remove_referenceIT_E4typeEOS3_(i64* nonnull align 8 dereferenceable(8) %6) #7
  %13 = load i64, i64* %12, align 8, !tbaa !3
  store i64 %13, i64* %11, align 8, !tbaa !19
  %14 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 3
  store i64 0, i64* %14, align 8, !tbaa !14
  %15 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 4
  store i64 1, i64* %15, align 8, !tbaa !15
  %16 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 0
  %17 = load i64, i64* %16, align 8, !tbaa !13
  %18 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 1
  %19 = load i64, i64* %18, align 8, !tbaa !19
  %20 = mul nsw i64 %17, %19
  %21 = invoke noalias nonnull i8* @_Znam(i64 %20) #8
          to label %22 unwind label %24

22:                                               ; preds = %3
  %23 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 2
  store i8* %21, i8** %23, align 8, !tbaa !11
  ret void

24:                                               ; preds = %3
  %25 = landingpad { i8*, i32 }
          catch i8* null
  %26 = extractvalue { i8*, i32 } %25, 0
  call void @__clang_call_terminate(i8* %26) #10
  unreachable
}

; Function Attrs: inlinehint nounwind ssp uwtable
define linkonce_odr hidden nonnull align 8 dereferenceable(8) i64* @_ZNSt3__14moveIRxEEONS_16remove_referenceIT_E4typeEOS3_(i64* nonnull align 8 dereferenceable(8) %0) #5 {
  %2 = alloca i64*, align 8
  store i64* %0, i64** %2, align 8, !tbaa !7
  %3 = load i64*, i64** %2, align 8, !tbaa !7
  ret i64* %3
}

declare i32 @__gxx_personality_v0(...)

; Function Attrs: noinline noreturn nounwind
define linkonce_odr hidden void @__clang_call_terminate(i8* %0) #6 {
  %2 = call i8* @__cxa_begin_catch(i8* %0) #7
  call void @_ZSt9terminatev() #10
  unreachable
}

declare i8* @__cxa_begin_catch(i8*)

declare void @_ZSt9terminatev()

; Function Attrs: nounwind ssp uwtable
define linkonce_odr void @_ZN5ArrayD2Ev(%class.Array* %0) unnamed_addr #3 align 2 {
  %2 = alloca %class.Array*, align 8
  store %class.Array* %0, %class.Array** %2, align 8, !tbaa !7
  %3 = load %class.Array*, %class.Array** %2, align 8
  %4 = getelementptr inbounds %class.Array, %class.Array* %3, i32 0, i32 2
  %5 = load i8*, i8** %4, align 8, !tbaa !11
  %6 = icmp eq i8* %5, null
  br i1 %6, label %8, label %7

7:                                                ; preds = %1
  call void @_ZdaPv(i8* %5) #9
  br label %8

8:                                                ; preds = %7, %1
  ret void
}

; Function Attrs: nobuiltin nounwind
declare void @_ZdaPv(i8*) #4

; Function Attrs: nounwind ssp uwtable
define linkonce_odr void @_ZN5ArrayC2EPS_(%class.Array* %0, %class.Array* %1) unnamed_addr #3 align 2 personality i8* bitcast (i32 (...)* @__gxx_personality_v0 to i8*) {
  %3 = alloca %class.Array*, align 8
  %4 = alloca %class.Array*, align 8
  store %class.Array* %0, %class.Array** %3, align 8, !tbaa !7
  store %class.Array* %1, %class.Array** %4, align 8, !tbaa !7
  %5 = load %class.Array*, %class.Array** %3, align 8
  %6 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 0
  %7 = load %class.Array*, %class.Array** %4, align 8, !tbaa !7
  %8 = getelementptr inbounds %class.Array, %class.Array* %7, i32 0, i32 0
  %9 = load i64, i64* %8, align 8, !tbaa !13
  store i64 %9, i64* %6, align 8, !tbaa !13
  %10 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 1
  %11 = load %class.Array*, %class.Array** %4, align 8, !tbaa !7
  %12 = getelementptr inbounds %class.Array, %class.Array* %11, i32 0, i32 1
  %13 = load i64, i64* %12, align 8, !tbaa !19
  store i64 %13, i64* %10, align 8, !tbaa !19
  %14 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 3
  store i64 0, i64* %14, align 8, !tbaa !14
  %15 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 4
  store i64 1, i64* %15, align 8, !tbaa !15
  %16 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 0
  %17 = load i64, i64* %16, align 8, !tbaa !13
  %18 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 1
  %19 = load i64, i64* %18, align 8, !tbaa !19
  %20 = mul nsw i64 %17, %19
  %21 = invoke noalias nonnull i8* @_Znam(i64 %20) #8
          to label %22 unwind label %34

22:                                               ; preds = %2
  %23 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 2
  store i8* %21, i8** %23, align 8, !tbaa !11
  %24 = load %class.Array*, %class.Array** %4, align 8, !tbaa !7
  %25 = getelementptr inbounds %class.Array, %class.Array* %24, i32 0, i32 2
  %26 = load i8*, i8** %25, align 8, !tbaa !11
  %27 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 2
  %28 = load i8*, i8** %27, align 8, !tbaa !11
  %29 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 0
  %30 = load i64, i64* %29, align 8, !tbaa !13
  %31 = getelementptr inbounds %class.Array, %class.Array* %5, i32 0, i32 1
  %32 = load i64, i64* %31, align 8, !tbaa !19
  %33 = mul nsw i64 %30, %32
  call void @llvm.memcpy.p0i8.p0i8.i64(i8* align 1 %26, i8* align 1 %28, i64 %33, i1 false)
  ret void

34:                                               ; preds = %2
  %35 = landingpad { i8*, i32 }
          catch i8* null
  %36 = extractvalue { i8*, i32 } %35, 0
  call void @__clang_call_terminate(i8* %36) #10
  unreachable
}

; Function Attrs: argmemonly nounwind willreturn
declare void @llvm.memcpy.p0i8.p0i8.i64(i8* noalias nocapture writeonly, i8* noalias nocapture readonly, i64, i1 immarg) #1

attributes #0 = { ssp uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "min-legal-vector-width"="0" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { argmemonly nounwind willreturn }
attributes #2 = { nobuiltin allocsize(0) "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #3 = { nounwind ssp uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "min-legal-vector-width"="0" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #4 = { nobuiltin nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #5 = { inlinehint nounwind ssp uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "min-legal-vector-width"="0" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #6 = { noinline noreturn nounwind }
attributes #7 = { nounwind }
attributes #8 = { builtin allocsize(0) }
attributes #9 = { builtin nounwind }
attributes #10 = { noreturn nounwind }

!llvm.module.flags = !{!0, !1}
!llvm.ident = !{!2}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{!"Homebrew clang version 11.1.0"}
!3 = !{!4, !4, i64 0}
!4 = !{!"long long", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{!8, !8, i64 0}
!8 = !{!"any pointer", !5, i64 0}
!9 = !{!10, !10, i64 0}
!10 = !{!"int", !5, i64 0}
!11 = !{!12, !8, i64 16}
!12 = !{!"_ZTS5Array", !4, i64 0, !4, i64 8, !8, i64 16, !4, i64 24, !4, i64 32}
!13 = !{!12, !4, i64 0}
!14 = !{!12, !4, i64 24}
!15 = !{!12, !4, i64 32}
!16 = !{!17, !17, i64 0}
!17 = !{!"bool", !5, i64 0}
!18 = !{i8 0, i8 2}
!19 = !{!12, !4, i64 8}
