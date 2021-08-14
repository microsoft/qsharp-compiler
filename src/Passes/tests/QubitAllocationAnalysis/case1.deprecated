; RUN:  opt -load-pass-plugin %shlibdir/libQubitAllocationAnalysis%shlibext -passes="print<qubit-allocation-analysis>" %S/inputs/static-qubit-arrays-1.ll -disable-output 2>&1\
; RUN:   | FileCheck %s

;------------------------------------------------------------------------------
; EXPECTED OUTPUT
;------------------------------------------------------------------------------

; CHECK: Example__QuantumProgram__body
; CHECK: ====================

; CHECK: qubits depends on x being constant to be static.




