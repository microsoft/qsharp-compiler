; ModuleID = 'src/stdlib.c'
source_filename = "src/stdlib.c"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

%struct.Callable = type { [4 x void (i8*, i8*, i8*)*]*, [2 x void (i8*, i32)*]*, i8*, i32, i32 }

; Function Attrs: noinline nounwind optnone ssp uwtable
define i8* @__quantum__rt__callable_create([4 x void (i8*, i8*, i8*)*]* %0, [2 x void (i8*, i32)*]* %1, i8* %2) #0 {
  %4 = alloca [4 x void (i8*, i8*, i8*)*]*, align 8
  %5 = alloca [2 x void (i8*, i32)*]*, align 8
  %6 = alloca i8*, align 8
  %7 = alloca %struct.Callable*, align 8
  store [4 x void (i8*, i8*, i8*)*]* %0, [4 x void (i8*, i8*, i8*)*]** %4, align 8
  store [2 x void (i8*, i32)*]* %1, [2 x void (i8*, i32)*]** %5, align 8
  store i8* %2, i8** %6, align 8
  %8 = call i8* @malloc(i64 32) #5
  %9 = bitcast i8* %8 to %struct.Callable*
  store %struct.Callable* %9, %struct.Callable** %7, align 8
  %10 = load [4 x void (i8*, i8*, i8*)*]*, [4 x void (i8*, i8*, i8*)*]** %4, align 8
  %11 = load %struct.Callable*, %struct.Callable** %7, align 8
  %12 = getelementptr inbounds %struct.Callable, %struct.Callable* %11, i32 0, i32 0
  store [4 x void (i8*, i8*, i8*)*]* %10, [4 x void (i8*, i8*, i8*)*]** %12, align 8
  %13 = load [2 x void (i8*, i32)*]*, [2 x void (i8*, i32)*]** %5, align 8
  %14 = load %struct.Callable*, %struct.Callable** %7, align 8
  %15 = getelementptr inbounds %struct.Callable, %struct.Callable* %14, i32 0, i32 1
  store [2 x void (i8*, i32)*]* %13, [2 x void (i8*, i32)*]** %15, align 8
  %16 = load i8*, i8** %6, align 8
  %17 = load %struct.Callable*, %struct.Callable** %7, align 8
  %18 = getelementptr inbounds %struct.Callable, %struct.Callable* %17, i32 0, i32 2
  store i8* %16, i8** %18, align 8
  %19 = load %struct.Callable*, %struct.Callable** %7, align 8
  %20 = getelementptr inbounds %struct.Callable, %struct.Callable* %19, i32 0, i32 3
  store i32 1, i32* %20, align 8
  %21 = load %struct.Callable*, %struct.Callable** %7, align 8
  %22 = getelementptr inbounds %struct.Callable, %struct.Callable* %21, i32 0, i32 4
  store i32 0, i32* %22, align 4
  %23 = load %struct.Callable*, %struct.Callable** %7, align 8
  %24 = bitcast %struct.Callable* %23 to i8*
  ret i8* %24
}

; Function Attrs: allocsize(0)
declare i8* @malloc(i64) #1

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__callable_update_alias_count(%struct.Callable* %0, i32 %1) #0 {
  %3 = alloca %struct.Callable*, align 8
  %4 = alloca i32, align 4
  store %struct.Callable* %0, %struct.Callable** %3, align 8
  store i32 %1, i32* %4, align 4
  %5 = load i32, i32* %4, align 4
  %6 = load %struct.Callable*, %struct.Callable** %3, align 8
  %7 = getelementptr inbounds %struct.Callable, %struct.Callable* %6, i32 0, i32 4
  %8 = load i32, i32* %7, align 4
  %9 = add nsw i32 %8, %5
  store i32 %9, i32* %7, align 4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__capture_update_alias_count(%struct.Callable* %0, i32 %1) #0 {
  %3 = alloca %struct.Callable*, align 8
  %4 = alloca i32, align 4
  store %struct.Callable* %0, %struct.Callable** %3, align 8
  store i32 %1, i32* %4, align 4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__capture_update_reference_count(%struct.Callable* %0, i32 %1) #0 {
  %3 = alloca %struct.Callable*, align 8
  %4 = alloca i32, align 4
  store %struct.Callable* %0, %struct.Callable** %3, align 8
  store i32 %1, i32* %4, align 4
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__callable_update_reference_count(%struct.Callable* %0, i32 %1) #0 {
  %3 = alloca %struct.Callable*, align 8
  %4 = alloca i32, align 4
  store %struct.Callable* %0, %struct.Callable** %3, align 8
  store i32 %1, i32* %4, align 4
  %5 = load i32, i32* %4, align 4
  %6 = load %struct.Callable*, %struct.Callable** %3, align 8
  %7 = getelementptr inbounds %struct.Callable, %struct.Callable* %6, i32 0, i32 3
  %8 = load i32, i32* %7, align 8
  %9 = add nsw i32 %8, %5
  store i32 %9, i32* %7, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__callable_invoke(%struct.Callable* %0, i8* %1, i8* %2) #0 {
  %4 = alloca %struct.Callable*, align 8
  %5 = alloca i8*, align 8
  %6 = alloca i8*, align 8
  store %struct.Callable* %0, %struct.Callable** %4, align 8
  store i8* %1, i8** %5, align 8
  store i8* %2, i8** %6, align 8
  %7 = load %struct.Callable*, %struct.Callable** %4, align 8
  %8 = getelementptr inbounds %struct.Callable, %struct.Callable* %7, i32 0, i32 0
  %9 = load [4 x void (i8*, i8*, i8*)*]*, [4 x void (i8*, i8*, i8*)*]** %8, align 8
  %10 = getelementptr inbounds [4 x void (i8*, i8*, i8*)*], [4 x void (i8*, i8*, i8*)*]* %9, i64 0, i64 0
  %11 = load void (i8*, i8*, i8*)*, void (i8*, i8*, i8*)** %10, align 8
  %12 = load %struct.Callable*, %struct.Callable** %4, align 8
  %13 = getelementptr inbounds %struct.Callable, %struct.Callable* %12, i32 0, i32 2
  %14 = load i8*, i8** %13, align 8
  %15 = load i8*, i8** %5, align 8
  %16 = load i8*, i8** %6, align 8
  call void %11(i8* %14, i8* %15, i8* %16)
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define i8* @__quantum__rt__tuple_create(i64 %0) #0 {
  %2 = alloca i64, align 8
  %3 = alloca i8*, align 8
  %4 = alloca i64*, align 8
  %5 = alloca i64*, align 8
  %6 = alloca i64*, align 8
  store i64 %0, i64* %2, align 8
  %7 = load i64, i64* %2, align 8
  %8 = add i64 %7, 24
  %9 = call i8* @malloc(i64 %8) #5
  store i8* %9, i8** %3, align 8
  %10 = load i8*, i8** %3, align 8
  %11 = bitcast i8* %10 to i64*
  store i64* %11, i64** %4, align 8
  %12 = load i8*, i8** %3, align 8
  %13 = getelementptr inbounds i8, i8* %12, i64 8
  %14 = bitcast i8* %13 to i64*
  store i64* %14, i64** %5, align 8
  %15 = load i8*, i8** %3, align 8
  %16 = getelementptr inbounds i8, i8* %15, i64 16
  %17 = bitcast i8* %16 to i64*
  store i64* %17, i64** %6, align 8
  %18 = load i64, i64* %2, align 8
  %19 = load i64*, i64** %4, align 8
  store i64 %18, i64* %19, align 8
  %20 = load i64*, i64** %5, align 8
  store i64 1, i64* %20, align 8
  %21 = load i64*, i64** %6, align 8
  store i64 0, i64* %21, align 8
  %22 = load i8*, i8** %3, align 8
  %23 = getelementptr inbounds i8, i8* %22, i64 24
  ret i8* %23
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__tuple_update_reference_count(i8* %0, i32 %1) #0 {
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i8*, align 8
  %6 = alloca i64*, align 8
  store i8* %0, i8** %3, align 8
  store i32 %1, i32* %4, align 4
  %7 = load i8*, i8** %3, align 8
  %8 = getelementptr inbounds i8, i8* %7, i64 -24
  store i8* %8, i8** %5, align 8
  %9 = load i8*, i8** %5, align 8
  %10 = getelementptr inbounds i8, i8* %9, i64 8
  %11 = bitcast i8* %10 to i64*
  store i64* %11, i64** %6, align 8
  %12 = load i32, i32* %4, align 4
  %13 = sext i32 %12 to i64
  %14 = load i64*, i64** %6, align 8
  %15 = load i64, i64* %14, align 8
  %16 = add nsw i64 %15, %13
  store i64 %16, i64* %14, align 8
  %17 = load i64*, i64** %6, align 8
  %18 = load i64, i64* %17, align 8
  %19 = icmp sle i64 %18, 0
  br i1 %19, label %20, label %22

20:                                               ; preds = %2
  %21 = load i8*, i8** %5, align 8
  call void @free(i8* %21)
  br label %22

22:                                               ; preds = %20, %2
  ret void
}

declare void @free(i8*) #2

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__tuple_update_alias_count(i8* %0, i32 %1) #0 {
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i8*, align 8
  %6 = alloca i64*, align 8
  store i8* %0, i8** %3, align 8
  store i32 %1, i32* %4, align 4
  %7 = load i8*, i8** %3, align 8
  %8 = getelementptr inbounds i8, i8* %7, i64 -24
  store i8* %8, i8** %5, align 8
  %9 = load i8*, i8** %5, align 8
  %10 = getelementptr inbounds i8, i8* %9, i64 24
  %11 = bitcast i8* %10 to i64*
  store i64* %11, i64** %6, align 8
  %12 = load i32, i32* %4, align 4
  %13 = sext i32 %12 to i64
  %14 = load i64*, i64** %6, align 8
  %15 = load i64, i64* %14, align 8
  %16 = add nsw i64 %15, %13
  store i64 %16, i64* %14, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define i8* @__quantum__rt__array_create_1d(i32 %0, i64 %1) #0 {
  %3 = alloca i32, align 4
  %4 = alloca i64, align 8
  %5 = alloca i8*, align 8
  %6 = alloca i64*, align 8
  %7 = alloca i64*, align 8
  %8 = alloca i64*, align 8
  %9 = alloca i64*, align 8
  store i32 %0, i32* %3, align 4
  store i64 %1, i64* %4, align 8
  %10 = load i64, i64* %4, align 8
  %11 = load i32, i32* %3, align 4
  %12 = sext i32 %11 to i64
  %13 = mul nsw i64 %10, %12
  %14 = add i64 %13, 32
  %15 = call i8* @malloc(i64 %14) #5
  store i8* %15, i8** %5, align 8
  %16 = load i8*, i8** %5, align 8
  %17 = bitcast i8* %16 to i64*
  store i64* %17, i64** %6, align 8
  %18 = load i8*, i8** %5, align 8
  %19 = getelementptr inbounds i8, i8* %18, i64 8
  %20 = bitcast i8* %19 to i64*
  store i64* %20, i64** %7, align 8
  %21 = load i8*, i8** %5, align 8
  %22 = getelementptr inbounds i8, i8* %21, i64 16
  %23 = bitcast i8* %22 to i64*
  store i64* %23, i64** %8, align 8
  %24 = load i8*, i8** %5, align 8
  %25 = getelementptr inbounds i8, i8* %24, i64 24
  %26 = bitcast i8* %25 to i64*
  store i64* %26, i64** %9, align 8
  %27 = load i32, i32* %3, align 4
  %28 = sext i32 %27 to i64
  %29 = load i64*, i64** %6, align 8
  store i64 %28, i64* %29, align 8
  %30 = load i64, i64* %4, align 8
  %31 = load i64*, i64** %7, align 8
  store i64 %30, i64* %31, align 8
  %32 = load i64*, i64** %8, align 8
  store i64 1, i64* %32, align 8
  %33 = load i64*, i64** %9, align 8
  store i64 0, i64* %33, align 8
  %34 = load i8*, i8** %5, align 8
  ret i8* %34
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define i8* @__quantum__rt__array_concatenate(i8* %0, i8* %1) #0 {
  %3 = alloca i8*, align 8
  %4 = alloca i8*, align 8
  %5 = alloca i64, align 8
  %6 = alloca i64, align 8
  %7 = alloca i64, align 8
  %8 = alloca i8*, align 8
  %9 = alloca i8*, align 8
  store i8* %0, i8** %3, align 8
  store i8* %1, i8** %4, align 8
  %10 = load i8*, i8** %3, align 8
  %11 = bitcast i8* %10 to i64*
  %12 = load i64, i64* %11, align 8
  store i64 %12, i64* %5, align 8
  %13 = load i8*, i8** %3, align 8
  %14 = getelementptr inbounds i8, i8* %13, i64 8
  %15 = bitcast i8* %14 to i64*
  %16 = load i64, i64* %15, align 8
  store i64 %16, i64* %6, align 8
  %17 = load i8*, i8** %4, align 8
  %18 = getelementptr inbounds i8, i8* %17, i64 8
  %19 = bitcast i8* %18 to i64*
  %20 = load i64, i64* %19, align 8
  store i64 %20, i64* %7, align 8
  %21 = load i8*, i8** %3, align 8
  %22 = getelementptr inbounds i8, i8* %21, i64 32
  store i8* %22, i8** %3, align 8
  %23 = load i8*, i8** %4, align 8
  %24 = getelementptr inbounds i8, i8* %23, i64 32
  store i8* %24, i8** %4, align 8
  %25 = load i64, i64* %5, align 8
  %26 = trunc i64 %25 to i32
  %27 = load i64, i64* %6, align 8
  %28 = load i64, i64* %7, align 8
  %29 = add nsw i64 %27, %28
  %30 = call i8* @__quantum__rt__array_create_1d(i32 %26, i64 %29)
  store i8* %30, i8** %8, align 8
  %31 = load i8*, i8** %8, align 8
  %32 = getelementptr inbounds i8, i8* %31, i64 32
  store i8* %32, i8** %9, align 8
  %33 = load i8*, i8** %9, align 8
  %34 = load i8*, i8** %3, align 8
  %35 = load i64, i64* %6, align 8
  %36 = load i64, i64* %5, align 8
  %37 = mul nsw i64 %35, %36
  %38 = load i8*, i8** %9, align 8
  %39 = call i64 @llvm.objectsize.i64.p0i8(i8* %38, i1 false, i1 true, i1 false)
  %40 = call i8* @__memcpy_chk(i8* %33, i8* %34, i64 %37, i64 %39) #6
  %41 = load i8*, i8** %9, align 8
  %42 = load i64, i64* %6, align 8
  %43 = load i64, i64* %5, align 8
  %44 = mul nsw i64 %42, %43
  %45 = getelementptr inbounds i8, i8* %41, i64 %44
  %46 = load i8*, i8** %4, align 8
  %47 = load i64, i64* %7, align 8
  %48 = load i64, i64* %5, align 8
  %49 = mul nsw i64 %47, %48
  %50 = load i8*, i8** %9, align 8
  %51 = load i64, i64* %6, align 8
  %52 = load i64, i64* %5, align 8
  %53 = mul nsw i64 %51, %52
  %54 = getelementptr inbounds i8, i8* %50, i64 %53
  %55 = call i64 @llvm.objectsize.i64.p0i8(i8* %54, i1 false, i1 true, i1 false)
  %56 = call i8* @__memcpy_chk(i8* %45, i8* %46, i64 %49, i64 %55) #6
  %57 = load i8*, i8** %8, align 8
  ret i8* %57
}

; Function Attrs: nounwind
declare i8* @__memcpy_chk(i8*, i8*, i64, i64) #3

; Function Attrs: nounwind readnone speculatable willreturn
declare i64 @llvm.objectsize.i64.p0i8(i8*, i1 immarg, i1 immarg, i1 immarg) #4

; Function Attrs: noinline nounwind optnone ssp uwtable
define i8* @__quantum__rt__array_copy(i8* %0, i8 signext %1) #0 {
  %3 = alloca i8*, align 8
  %4 = alloca i8*, align 8
  %5 = alloca i8, align 1
  %6 = alloca i64*, align 8
  %7 = alloca i64*, align 8
  %8 = alloca i64*, align 8
  %9 = alloca i8*, align 8
  store i8* %0, i8** %4, align 8
  store i8 %1, i8* %5, align 1
  %10 = load i8*, i8** %4, align 8
  %11 = icmp eq i8* %10, null
  br i1 %11, label %12, label %13

12:                                               ; preds = %2
  store i8* null, i8** %3, align 8
  br label %49

13:                                               ; preds = %2
  %14 = load i8*, i8** %4, align 8
  %15 = bitcast i8* %14 to i64*
  store i64* %15, i64** %6, align 8
  %16 = load i8*, i8** %4, align 8
  %17 = getelementptr inbounds i8, i8* %16, i64 8
  %18 = bitcast i8* %17 to i64*
  store i64* %18, i64** %7, align 8
  %19 = load i8*, i8** %4, align 8
  %20 = getelementptr inbounds i8, i8* %19, i64 24
  %21 = bitcast i8* %20 to i64*
  store i64* %21, i64** %8, align 8
  %22 = load i8, i8* %5, align 1
  %23 = sext i8 %22 to i32
  %24 = icmp ne i32 %23, 0
  br i1 %24, label %29, label %25

25:                                               ; preds = %13
  %26 = load i64*, i64** %8, align 8
  %27 = load i64, i64* %26, align 8
  %28 = icmp sgt i64 %27, 0
  br i1 %28, label %29, label %47

29:                                               ; preds = %25, %13
  %30 = load i64*, i64** %6, align 8
  %31 = load i64, i64* %30, align 8
  %32 = trunc i64 %31 to i32
  %33 = load i64*, i64** %7, align 8
  %34 = load i64, i64* %33, align 8
  %35 = call i8* @__quantum__rt__array_create_1d(i32 %32, i64 %34)
  store i8* %35, i8** %9, align 8
  %36 = load i8*, i8** %9, align 8
  %37 = load i8*, i8** %4, align 8
  %38 = load i64*, i64** %6, align 8
  %39 = load i64, i64* %38, align 8
  %40 = load i64*, i64** %7, align 8
  %41 = load i64, i64* %40, align 8
  %42 = mul nsw i64 %39, %41
  %43 = load i8*, i8** %9, align 8
  %44 = call i64 @llvm.objectsize.i64.p0i8(i8* %43, i1 false, i1 true, i1 false)
  %45 = call i8* @__memcpy_chk(i8* %36, i8* %37, i64 %42, i64 %44) #6
  %46 = load i8*, i8** %9, align 8
  store i8* %46, i8** %3, align 8
  br label %49

47:                                               ; preds = %25
  %48 = load i8*, i8** %4, align 8
  store i8* %48, i8** %3, align 8
  br label %49

49:                                               ; preds = %47, %29, %12
  %50 = load i8*, i8** %3, align 8
  ret i8* %50
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define i64 @__quantum__rt__array_get_size_1d(i8* %0) #0 {
  %2 = alloca i8*, align 8
  %3 = alloca i64*, align 8
  store i8* %0, i8** %2, align 8
  %4 = load i8*, i8** %2, align 8
  %5 = getelementptr inbounds i8, i8* %4, i64 8
  %6 = bitcast i8* %5 to i64*
  store i64* %6, i64** %3, align 8
  %7 = load i64*, i64** %3, align 8
  %8 = load i64, i64* %7, align 8
  ret i64 %8
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define i8* @__quantum__rt__array_get_element_ptr_1d(i8* %0, i64 %1) #0 {
  %3 = alloca i8*, align 8
  %4 = alloca i64, align 8
  %5 = alloca i64*, align 8
  store i8* %0, i8** %3, align 8
  store i64 %1, i64* %4, align 8
  %6 = load i8*, i8** %3, align 8
  %7 = bitcast i8* %6 to i64*
  store i64* %7, i64** %5, align 8
  %8 = load i8*, i8** %3, align 8
  %9 = load i64*, i64** %5, align 8
  %10 = load i64, i64* %9, align 8
  %11 = load i64, i64* %4, align 8
  %12 = mul nsw i64 %10, %11
  %13 = getelementptr inbounds i8, i8* %8, i64 %12
  %14 = getelementptr inbounds i8, i8* %13, i64 32
  ret i8* %14
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__array_update_alias_count(i8* %0, i32 %1) #0 {
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i64*, align 8
  store i8* %0, i8** %3, align 8
  store i32 %1, i32* %4, align 4
  %6 = load i8*, i8** %3, align 8
  %7 = getelementptr inbounds i8, i8* %6, i64 24
  %8 = bitcast i8* %7 to i64*
  store i64* %8, i64** %5, align 8
  %9 = load i32, i32* %4, align 4
  %10 = sext i32 %9 to i64
  %11 = load i64*, i64** %5, align 8
  %12 = load i64, i64* %11, align 8
  %13 = add nsw i64 %12, %10
  store i64 %13, i64* %11, align 8
  ret void
}

; Function Attrs: noinline nounwind optnone ssp uwtable
define void @__quantum__rt__array_update_reference_count(i8* %0, i32 %1) #0 {
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i64*, align 8
  store i8* %0, i8** %3, align 8
  store i32 %1, i32* %4, align 4
  %6 = load i8*, i8** %3, align 8
  %7 = getelementptr inbounds i8, i8* %6, i64 16
  %8 = bitcast i8* %7 to i64*
  store i64* %8, i64** %5, align 8
  %9 = load i32, i32* %4, align 4
  %10 = sext i32 %9 to i64
  %11 = load i64*, i64** %5, align 8
  %12 = load i64, i64* %11, align 8
  %13 = add nsw i64 %12, %10
  store i64 %13, i64* %11, align 8
  %14 = load i64*, i64** %5, align 8
  %15 = load i64, i64* %14, align 8
  %16 = icmp sle i64 %15, 0
  br i1 %16, label %17, label %19

17:                                               ; preds = %2
  %18 = load i8*, i8** %3, align 8
  call void @free(i8* %18)
  br label %19

19:                                               ; preds = %17, %2
  ret void
}

attributes #0 = { noinline nounwind optnone ssp uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "min-legal-vector-width"="0" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { allocsize(0) "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #2 = { "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #3 = { nounwind "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #4 = { nounwind readnone speculatable willreturn }
attributes #5 = { allocsize(0) }
attributes #6 = { nounwind }

!llvm.module.flags = !{!0, !1}
!llvm.ident = !{!2}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{!"Homebrew clang version 11.1.0"}
