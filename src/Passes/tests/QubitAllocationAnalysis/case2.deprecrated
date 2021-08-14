; RUN:  opt -load-pass-plugin %shlibdir/libQubitAllocationAnalysis%shlibext -passes="print<qubit-allocation-analysis>" %S/inputs/static-qubit-arrays-2.ll -disable-output 2>&1\
; RUN:   | FileCheck %s

;------------------------------------------------------------------------------
; EXPECTED OUTPUT
;------------------------------------------------------------------------------

; CHECK: Example__QuantumProgram__body
; CHECK: ====================
; CHECK: qubits0 is trivially static with 9 qubits.
; CHECK: qubits1 depends on x being constant to be static.
; CHECK: qubits2 depends on x, g being constant to be static.
; CHECK: qubits3 depends on h being constant to be static.
; CHECK: qubits4 is dynamic.
