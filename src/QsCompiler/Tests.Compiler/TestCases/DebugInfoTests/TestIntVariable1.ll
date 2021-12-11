define internal i64 @Microsoft__Quantum__Testing__QirDebugInfo__Main__body() !dbg !7 {
entry:
  tail call void @llvm.dbg.value(metadata i64 42, metadata !13, metadata !DIExpression()), !dbg !15
  %var_y = alloca i64, align 8, !dbg !16
  store i64 43, i64* %var_y, align 4, !dbg !16
  call void @llvm.dbg.declare(metadata i64* %var_y, metadata !14, metadata !DIExpression()), !dbg !16
  store i64 42, i64* %var_y, align 4, !dbg !17
  ret i64 42, !dbg !18
}
