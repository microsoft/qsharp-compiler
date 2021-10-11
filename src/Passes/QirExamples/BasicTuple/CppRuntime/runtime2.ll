; ModuleID = 'altruntime.c'
source_filename = "altruntime.c"
target datalayout = "e-m:o-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-apple-macosx11.0.0"

; Function Attrs: nounwind ssp uwtable
define i8* @__quantum__rt__tuple_create(i64 %0) #0 {
  %2 = alloca i64, align 8
  %3 = alloca i8*, align 8
  %4 = alloca i64*, align 8
  %5 = alloca i64*, align 8
  %6 = alloca i64*, align 8
  store i64 %0, i64* %2, align 8, !tbaa !3
  %7 = bitcast i8** %3 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %7) #4
  %8 = load i64, i64* %2, align 8, !tbaa !3
  %9 = add i64 %8, 24
  %10 = call i8* @malloc(i64 %9) #5
  store i8* %10, i8** %3, align 8, !tbaa !7
  %11 = bitcast i64** %4 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %11) #4
  %12 = load i8*, i8** %3, align 8, !tbaa !7
  %13 = bitcast i8* %12 to i64*
  store i64* %13, i64** %4, align 8, !tbaa !7
  %14 = bitcast i64** %5 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %14) #4
  %15 = load i8*, i8** %3, align 8, !tbaa !7
  %16 = getelementptr inbounds i8, i8* %15, i64 8
  %17 = bitcast i8* %16 to i64*
  store i64* %17, i64** %5, align 8, !tbaa !7
  %18 = bitcast i64** %6 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %18) #4
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
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %28) #4
  %29 = bitcast i64** %5 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %29) #4
  %30 = bitcast i64** %4 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %30) #4
  %31 = bitcast i8** %3 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %31) #4
  ret i8* %27
}

; Function Attrs: argmemonly nounwind willreturn
declare void @llvm.lifetime.start.p0i8(i64 immarg, i8* nocapture) #1

; Function Attrs: allocsize(0)
declare i8* @malloc(i64) #2

; Function Attrs: argmemonly nounwind willreturn
declare void @llvm.lifetime.end.p0i8(i64 immarg, i8* nocapture) #1

; Function Attrs: nounwind ssp uwtable
define void @__quantum__rt__tuple_update_reference_count(i8* %0, i32 %1) #0 {
  %3 = alloca i8*, align 8
  %4 = alloca i32, align 4
  %5 = alloca i8*, align 8
  %6 = alloca i64*, align 8
  store i8* %0, i8** %3, align 8, !tbaa !7
  store i32 %1, i32* %4, align 4, !tbaa !9
  %7 = bitcast i8** %5 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %7) #4
  %8 = load i8*, i8** %3, align 8, !tbaa !7
  %9 = getelementptr inbounds i8, i8* %8, i64 -24
  store i8* %9, i8** %5, align 8, !tbaa !7
  %10 = bitcast i64** %6 to i8*
  call void @llvm.lifetime.start.p0i8(i64 8, i8* %10) #4
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
  br i1 %21, label %22, label %24

22:                                               ; preds = %2
  %23 = load i8*, i8** %5, align 8, !tbaa !7
  call void @free(i8* %23)
  br label %24

24:                                               ; preds = %22, %2
  %25 = bitcast i64** %6 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %25) #4
  %26 = bitcast i8** %5 to i8*
  call void @llvm.lifetime.end.p0i8(i64 8, i8* %26) #4
  ret void
}

declare void @free(i8*) #3

attributes #0 = { nounwind ssp uwtable "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "min-legal-vector-width"="0" "no-infs-fp-math"="false" "no-jump-tables"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #1 = { argmemonly nounwind willreturn }
attributes #2 = { allocsize(0) "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #3 = { "correctly-rounded-divide-sqrt-fp-math"="false" "disable-tail-calls"="false" "frame-pointer"="all" "less-precise-fpmad"="false" "no-infs-fp-math"="false" "no-nans-fp-math"="false" "no-signed-zeros-fp-math"="false" "no-trapping-math"="true" "stack-protector-buffer-size"="8" "target-cpu"="penryn" "target-features"="+cx16,+cx8,+fxsr,+mmx,+sahf,+sse,+sse2,+sse3,+sse4.1,+ssse3,+x87" "unsafe-fp-math"="false" "use-soft-float"="false" }
attributes #4 = { nounwind }
attributes #5 = { allocsize(0) }

!llvm.module.flags = !{!0, !1}
!llvm.ident = !{!2}

!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!2 = !{!"Homebrew clang version 11.1.0"}
!3 = !{!4, !4, i64 0}
!4 = !{!"long long", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C/C++ TBAA"}
!7 = !{!8, !8, i64 0}
!8 = !{!"any pointer", !5, i64 0}
!9 = !{!10, !10, i64 0}
!10 = !{!"int", !5, i64 0}
