define internal i1 @Microsoft__Quantum__Testing__QirDebugInfo__Main__body() !dbg !7 {
entry:
  tail call void @llvm.dbg.value(metadata i1 true, metadata !13, metadata !DIExpression()), !dbg !15
  %var_y = alloca i1, align 1, !dbg !16
  store i1 false, i1* %var_y, align 1, !dbg !16
  call void @llvm.dbg.declare(metadata i1* %var_y, metadata !14, metadata !DIExpression()), !dbg !16
  store i1 true, i1* %var_y, align 1, !dbg !17
  ret i1 true, !dbg !18
}