define internal i64 @Microsoft__Quantum__Testing__QirDebugInfo__Main__body() !dbg !7 {
entry:
  %boolean = alloca i1, align 1, !dbg !15
  store i1 true, i1* %boolean, align 1, !dbg !15
  call void @llvm.dbg.declare(metadata i1* %boolean, metadata !13, metadata !DIExpression()), !dbg !15
  br i1 false, label %then0__1, label %continue__1, !dbg !16

then0__1:                                         ; preds = %entry
  ret i64 1, !dbg !17

continue__1:                                      ; preds = %entry
  store i1 false, i1* %boolean, align 1, !dbg !18
  br i1 true, label %then0__2, label %continue__2, !dbg !19

then0__2:                                         ; preds = %continue__1
  ret i64 0, !dbg !20

continue__2:                                      ; preds = %continue__1
  ret i64 1, !dbg !21
}
