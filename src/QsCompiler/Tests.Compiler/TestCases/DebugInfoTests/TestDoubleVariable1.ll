define internal double @Microsoft__Quantum__Testing__QirDebugInfo__Main__body() !dbg !7 {
entry:
  tail call void @llvm.dbg.value(metadata double 4.210000e+01, metadata !13, metadata !DIExpression()), !dbg !15
  %var_y = alloca double, align 8, !dbg !16
  store double 4.330000e+01, double* %var_y, align 8, !dbg !16
  call void @llvm.dbg.declare(metadata double* %var_y, metadata !14, metadata !DIExpression()), !dbg !16
  store double 4.210000e+01, double* %var_y, align 8, !dbg !17
  ret double 4.210000e+01, !dbg !18
}
