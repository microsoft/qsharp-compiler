	.text
	.def	 @feat.00;
	.scl	3;
	.type	0;
	.endef
	.globl	@feat.00
.set @feat.00, 0
	.file	"QRNG.ll"
	.def	 Microsoft__Quantum__Intrinsic__CNOT__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__CNOT__body # -- Begin function Microsoft__Quantum__Intrinsic__CNOT__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__CNOT__body: # @Microsoft__Quantum__Intrinsic__CNOT__body
.seh_proc Microsoft__Quantum__Intrinsic__CNOT__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__cnot
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__H__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__H__body # -- Begin function Microsoft__Quantum__Intrinsic__H__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__H__body: # @Microsoft__Quantum__Intrinsic__H__body
.seh_proc Microsoft__Quantum__Intrinsic__H__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__h
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__IntAsDouble__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__IntAsDouble__body # -- Begin function Microsoft__Quantum__Intrinsic__IntAsDouble__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__IntAsDouble__body: # @Microsoft__Quantum__Intrinsic__IntAsDouble__body
.seh_proc Microsoft__Quantum__Intrinsic__IntAsDouble__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__intAsDouble
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__Measure__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__Measure__body # -- Begin function Microsoft__Quantum__Intrinsic__Measure__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__Measure__body: # @Microsoft__Quantum__Intrinsic__Measure__body
.seh_proc Microsoft__Quantum__Intrinsic__Measure__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__measure
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__Mz__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__Mz__body # -- Begin function Microsoft__Quantum__Intrinsic__Mz__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__Mz__body: # @Microsoft__Quantum__Intrinsic__Mz__body
.seh_proc Microsoft__Quantum__Intrinsic__Mz__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__mz
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__Rx__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__Rx__body # -- Begin function Microsoft__Quantum__Intrinsic__Rx__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__Rx__body: # @Microsoft__Quantum__Intrinsic__Rx__body
.seh_proc Microsoft__Quantum__Intrinsic__Rx__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__rx
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__Rx__adj;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__Rx__adj # -- Begin function Microsoft__Quantum__Intrinsic__Rx__adj
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__Rx__adj: # @Microsoft__Quantum__Intrinsic__Rx__adj
.seh_proc Microsoft__Quantum__Intrinsic__Rx__adj
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	movq	%xmm0, %rax
	movabsq	$-9223372036854775808, %rcx     # imm = 0x8000000000000000
	xorq	%rcx, %rax
	movq	%rax, %xmm0
	callq	__quantum__qis__rx
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__Rz__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__Rz__body # -- Begin function Microsoft__Quantum__Intrinsic__Rz__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__Rz__body: # @Microsoft__Quantum__Intrinsic__Rz__body
.seh_proc Microsoft__Quantum__Intrinsic__Rz__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__rz
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__Rz__adj;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__Rz__adj # -- Begin function Microsoft__Quantum__Intrinsic__Rz__adj
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__Rz__adj: # @Microsoft__Quantum__Intrinsic__Rz__adj
.seh_proc Microsoft__Quantum__Intrinsic__Rz__adj
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	movq	%xmm0, %rax
	movabsq	$-9223372036854775808, %rcx     # imm = 0x8000000000000000
	xorq	%rcx, %rax
	movq	%rax, %xmm0
	callq	__quantum__qis__rz
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__S__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__S__body # -- Begin function Microsoft__Quantum__Intrinsic__S__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__S__body: # @Microsoft__Quantum__Intrinsic__S__body
.seh_proc Microsoft__Quantum__Intrinsic__S__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__s
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__S__adj;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__S__adj # -- Begin function Microsoft__Quantum__Intrinsic__S__adj
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__S__adj:  # @Microsoft__Quantum__Intrinsic__S__adj
.seh_proc Microsoft__Quantum__Intrinsic__S__adj
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	movq	%rcx, 32(%rsp)                  # 8-byte Spill
	callq	__quantum__qis__s
	movq	32(%rsp), %rcx                  # 8-byte Reload
	callq	__quantum__qis__z
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__X__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__X__body # -- Begin function Microsoft__Quantum__Intrinsic__X__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__X__body: # @Microsoft__Quantum__Intrinsic__X__body
.seh_proc Microsoft__Quantum__Intrinsic__X__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__x
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Intrinsic__Z__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Intrinsic__Z__body # -- Begin function Microsoft__Quantum__Intrinsic__Z__body
	.p2align	4, 0x90
Microsoft__Quantum__Intrinsic__Z__body: # @Microsoft__Quantum__Intrinsic__Z__body
.seh_proc Microsoft__Quantum__Intrinsic__Z__body
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	__quantum__qis__z
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Qrng__RandomBit__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Qrng__RandomBit__body           # -- Begin function Qrng__RandomBit__body
	.p2align	4, 0x90
Qrng__RandomBit__body:                  # @Qrng__RandomBit__body
.seh_proc Qrng__RandomBit__body
# %bb.0:                                # %entry
	subq	$136, %rsp
	.seh_stackalloc 136
	.seh_endprologue
	callq	__quantum__rt__qubit_allocate
	movq	%rax, %rcx
	movl	$1, %edx
	movl	$1, %r8d
	movq	%rcx, 128(%rsp)                 # 8-byte Spill
	movl	%edx, %ecx
	movl	%edx, 124(%rsp)                 # 4-byte Spill
	movq	%r8, %rdx
	movq	%rax, 112(%rsp)                 # 8-byte Spill
	movq	%r8, 104(%rsp)                  # 8-byte Spill
	callq	__quantum__rt__array_create_1d
	movq	%rax, %rdx
	xorl	%ecx, %ecx
	movl	%ecx, %r8d
	movq	%rax, %rcx
	movq	%rdx, 96(%rsp)                  # 8-byte Spill
	movq	%r8, %rdx
	movq	%rax, 88(%rsp)                  # 8-byte Spill
	movq	%r8, 80(%rsp)                   # 8-byte Spill
	callq	__quantum__rt__array_get_element_ptr_1d
	movb	PauliX(%rip), %r9b
	movb	%r9b, (%rax)
	movl	$8, %ecx
	movq	104(%rsp), %rdx                 # 8-byte Reload
	callq	__quantum__rt__array_create_1d
	movq	%rax, %rdx
	movq	%rax, %rcx
	movq	80(%rsp), %r8                   # 8-byte Reload
	movq	%rdx, 72(%rsp)                  # 8-byte Spill
	movq	%r8, %rdx
	movq	%rax, 64(%rsp)                  # 8-byte Spill
	callq	__quantum__rt__array_get_element_ptr_1d
	movq	112(%rsp), %rcx                 # 8-byte Reload
	movq	%rcx, (%rax)
	movq	88(%rsp), %rcx                  # 8-byte Reload
	movq	64(%rsp), %rdx                  # 8-byte Reload
	callq	__quantum__qis__measure
	movl	124(%rsp), %ecx                 # 4-byte Reload
	movq	104(%rsp), %rdx                 # 8-byte Reload
	movq	%rax, 56(%rsp)                  # 8-byte Spill
	callq	__quantum__rt__array_create_1d
	movq	%rax, %rdx
	movq	%rax, %rcx
	movq	80(%rsp), %rax                  # 8-byte Reload
	movq	%rdx, 48(%rsp)                  # 8-byte Spill
	movq	%rax, %rdx
	callq	__quantum__rt__array_get_element_ptr_1d
	movb	PauliZ(%rip), %r9b
	movb	%r9b, (%rax)
	movl	$8, %ecx
	movl	$1, %edx
	callq	__quantum__rt__array_create_1d
	xorl	%ecx, %ecx
	movl	%ecx, %edx
	movq	%rax, %rcx
	movq	%rax, 40(%rsp)                  # 8-byte Spill
	callq	__quantum__rt__array_get_element_ptr_1d
	movq	128(%rsp), %rcx                 # 8-byte Reload
	movq	%rcx, (%rax)
	movq	48(%rsp), %rcx                  # 8-byte Reload
	movq	40(%rsp), %rdx                  # 8-byte Reload
	callq	__quantum__qis__measure
	movq	128(%rsp), %rcx                 # 8-byte Reload
	movq	%rax, 32(%rsp)                  # 8-byte Spill
	callq	__quantum__rt__qubit_release
	movq	96(%rsp), %rcx                  # 8-byte Reload
	callq	__quantum__rt__array_unreference
	movq	72(%rsp), %rcx                  # 8-byte Reload
	callq	__quantum__rt__array_unreference
	movq	48(%rsp), %rcx                  # 8-byte Reload
	callq	__quantum__rt__array_unreference
	movq	40(%rsp), %rcx                  # 8-byte Reload
	callq	__quantum__rt__array_unreference
	movq	32(%rsp), %rax                  # 8-byte Reload
	addq	$136, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Qrng__RandomInt__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Qrng__RandomInt__body           # -- Begin function Qrng__RandomInt__body
	.p2align	4, 0x90
Qrng__RandomInt__body:                  # @Qrng__RandomInt__body
.seh_proc Qrng__RandomInt__body
# %bb.0:                                # %entry
	subq	$72, %rsp
	.seh_stackalloc 72
	.seh_endprologue
	movq	$0, 64(%rsp)
# %bb.1:                                # %preheader__1
	xorl	%eax, %eax
	movl	%eax, %ecx
	movq	%rcx, 56(%rsp)                  # 8-byte Spill
	jmp	.LBB14_2
.LBB14_2:                               # %header__1
                                        # =>This Inner Loop Header: Depth=1
	movq	56(%rsp), %rax                  # 8-byte Reload
	movq	%rax, %rcx
	subq	$32, %rcx
	setl	%dl
	testb	$1, %dl
	movq	%rax, 48(%rsp)                  # 8-byte Spill
	jne	.LBB14_3
	jmp	.LBB14_7
.LBB14_3:                               # %body__1
                                        #   in Loop: Header=BB14_2 Depth=1
	callq	Qrng__RandomBit__body
	movq	ResultOne(%rip), %rdx
	movq	%rax, %rcx
	movq	%rax, 40(%rsp)                  # 8-byte Spill
	callq	__quantum__rt__result_equal
	testb	$1, %al
	jne	.LBB14_4
	jmp	.LBB14_5
.LBB14_4:                               # %then0__1
                                        #   in Loop: Header=BB14_2 Depth=1
	movq	64(%rsp), %rax
	movq	48(%rsp), %rcx                  # 8-byte Reload
                                        # kill: def $cl killed $rcx
	movl	$1, %edx
	shlq	%cl, %rdx
	addq	%rdx, %rax
	movq	%rax, 64(%rsp)
.LBB14_5:                               # %continue__1
                                        #   in Loop: Header=BB14_2 Depth=1
	movq	40(%rsp), %rcx                  # 8-byte Reload
	callq	__quantum__rt__result_unreference
# %bb.6:                                # %exiting__1
                                        #   in Loop: Header=BB14_2 Depth=1
	movq	48(%rsp), %rax                  # 8-byte Reload
	addq	$1, %rax
	movq	%rax, 56(%rsp)                  # 8-byte Spill
	jmp	.LBB14_2
.LBB14_7:                               # %exit__1
	movq	64(%rsp), %rax
	addq	$72, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Qrng__RandomInts__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Qrng__RandomInts__body          # -- Begin function Qrng__RandomInts__body
	.p2align	4, 0x90
Qrng__RandomInts__body:                 # @Qrng__RandomInts__body
.seh_proc Qrng__RandomInts__body
# %bb.0:                                # %entry
	subq	$88, %rsp
	.seh_stackalloc 88
	.seh_endprologue
	movl	$8, %ecx
	movl	$32, %edx
	callq	__quantum__rt__array_create_1d
	movq	%rax, 80(%rsp)
	movq	%rax, 72(%rsp)                  # 8-byte Spill
# %bb.1:                                # %preheader__1
	xorl	%eax, %eax
	movl	%eax, %ecx
	movq	%rcx, 64(%rsp)                  # 8-byte Spill
	jmp	.LBB15_2
.LBB15_2:                               # %header__1
                                        # =>This Inner Loop Header: Depth=1
	movq	64(%rsp), %rax                  # 8-byte Reload
	movq	%rax, %rcx
	subq	$32, %rcx
	setl	%dl
	testb	$1, %dl
	movq	%rax, 56(%rsp)                  # 8-byte Spill
	jne	.LBB15_3
	jmp	.LBB15_5
.LBB15_3:                               # %body__1
                                        #   in Loop: Header=BB15_2 Depth=1
	movq	80(%rsp), %rcx
	callq	__quantum__rt__array_copy
	movq	%rax, 48(%rsp)                  # 8-byte Spill
	callq	Qrng__RandomInt__body
	movq	48(%rsp), %rcx                  # 8-byte Reload
	movq	56(%rsp), %rdx                  # 8-byte Reload
	movq	%rax, 40(%rsp)                  # 8-byte Spill
	callq	__quantum__rt__array_get_element_ptr_1d
	movq	40(%rsp), %rcx                  # 8-byte Reload
	movq	%rcx, (%rax)
	movq	48(%rsp), %rax                  # 8-byte Reload
	movq	%rax, 80(%rsp)
	movq	%rax, %rcx
	callq	__quantum__rt__array_reference
	movq	48(%rsp), %rcx                  # 8-byte Reload
	callq	__quantum__rt__array_unreference
# %bb.4:                                # %exiting__1
                                        #   in Loop: Header=BB15_2 Depth=1
	movq	56(%rsp), %rax                  # 8-byte Reload
	addq	$1, %rax
	movq	%rax, 64(%rsp)                  # 8-byte Spill
	jmp	.LBB15_2
.LBB15_5:                               # %exit__1
	movq	80(%rsp), %rax
	movq	72(%rsp), %rcx                  # 8-byte Reload
	movq	%rax, 32(%rsp)                  # 8-byte Spill
	callq	__quantum__rt__array_unreference
	movq	32(%rsp), %rax                  # 8-byte Reload
	addq	$88, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Microsoft__Quantum__Core__Attribute__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Core__Attribute__body # -- Begin function Microsoft__Quantum__Core__Attribute__body
	.p2align	4, 0x90
Microsoft__Quantum__Core__Attribute__body: # @Microsoft__Quantum__Core__Attribute__body
# %bb.0:                                # %entry
	xorl	%eax, %eax
                                        # kill: def $rax killed $eax
	retq
                                        # -- End function
	.def	 Microsoft__Quantum__Core__EntryPoint__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Core__EntryPoint__body # -- Begin function Microsoft__Quantum__Core__EntryPoint__body
	.p2align	4, 0x90
Microsoft__Quantum__Core__EntryPoint__body: # @Microsoft__Quantum__Core__EntryPoint__body
# %bb.0:                                # %entry
	xorl	%eax, %eax
                                        # kill: def $rax killed $eax
	retq
                                        # -- End function
	.def	 Microsoft__Quantum__Core__Inline__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Core__Inline__body # -- Begin function Microsoft__Quantum__Core__Inline__body
	.p2align	4, 0x90
Microsoft__Quantum__Core__Inline__body: # @Microsoft__Quantum__Core__Inline__body
# %bb.0:                                # %entry
	xorl	%eax, %eax
                                        # kill: def $rax killed $eax
	retq
                                        # -- End function
	.def	 Microsoft__Quantum__Core__Intrinsic__body;
	.scl	2;
	.type	32;
	.endef
	.globl	Microsoft__Quantum__Core__Intrinsic__body # -- Begin function Microsoft__Quantum__Core__Intrinsic__body
	.p2align	4, 0x90
Microsoft__Quantum__Core__Intrinsic__body: # @Microsoft__Quantum__Core__Intrinsic__body
.seh_proc Microsoft__Quantum__Core__Intrinsic__body
# %bb.0:                                # %entry
	subq	$56, %rsp
	.seh_stackalloc 56
	.seh_endprologue
	xorl	%eax, %eax
	movl	%eax, %edx
	addq	$16, %rdx
	movq	%rcx, 48(%rsp)                  # 8-byte Spill
	movq	%rdx, %rcx
	callq	__quantum__rt__tuple_create
	movq	%rax, %rcx
	movq	48(%rsp), %rdx                  # 8-byte Reload
	movq	%rdx, 8(%rax)
	movq	%rcx, 40(%rsp)                  # 8-byte Spill
	movq	%rdx, %rcx
	callq	__quantum__rt__string_reference
	movq	40(%rsp), %rax                  # 8-byte Reload
	addq	$56, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.def	 Qrng_RandomInts;
	.scl	2;
	.type	32;
	.endef
	.globl	Qrng_RandomInts                 # -- Begin function Qrng_RandomInts
	.p2align	4, 0x90
Qrng_RandomInts:                        # @Qrng_RandomInts
.seh_proc Qrng_RandomInts
# %bb.0:                                # %entry
	subq	$40, %rsp
	.seh_stackalloc 40
	.seh_endprologue
	callq	Qrng__RandomInts__body
	nop
	addq	$40, %rsp
	retq
	.seh_handlerdata
	.text
	.seh_endproc
                                        # -- End function
	.section	.rdata,"dr"
	.globl	PauliI                          # @PauliI
PauliI:
	.byte	0                               # 0x0

	.globl	PauliX                          # @PauliX
PauliX:
	.byte	1                               # 0x1

	.globl	PauliY                          # @PauliY
PauliY:
	.byte	3                               # 0x3

	.globl	PauliZ                          # @PauliZ
PauliZ:
	.byte	2                               # 0x2

	.p2align	4                               # @EmptyRange
EmptyRange:
	.quad	0                               # 0x0
	.quad	1                               # 0x1
	.quad	-1                              # 0xffffffffffffffff

	.globl	_fltused
