define internal double @Microsoft__Quantum__Testing__QirDebugInfo__Main__body() !dbg !7 {
entry:
  tail call void @llvm.dbg.value(metadata double 4.210000e+01, metadata !13, metadata !DIExpression()), !dbg !15
  %varY = alloca double, align 8, !dbg !16
  store double 4.330000e+01, double* %varY, align 8, !dbg !16
  call void @llvm.dbg.declare(metadata double* %varY, metadata !14, metadata !DIExpression()), !dbg !16
  store double 4.210000e+01, double* %varY, align 8, !dbg !17
  ret double 4.210000e+01, !dbg !18
}
