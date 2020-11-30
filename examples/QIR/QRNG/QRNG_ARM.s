	.text
	.syntax unified
	.eabi_attribute	67, "2.09"
	.cpu	cortex-m3
	.eabi_attribute	6, 10
	.eabi_attribute	7, 77
	.eabi_attribute	8, 0
	.eabi_attribute	9, 2
	.eabi_attribute	34, 0
	.eabi_attribute	17, 1
	.eabi_attribute	20, 1
	.eabi_attribute	21, 0
	.eabi_attribute	23, 3
	.eabi_attribute	24, 1
	.eabi_attribute	25, 1
	.eabi_attribute	28, 1
	.eabi_attribute	38, 1
	.eabi_attribute	14, 0
	.file	"QRNG.ll"
	.globl	Qrng__RandomBit__body
	.p2align	1
	.type	Qrng__RandomBit__body,%function
	.code	16
	.thumb_func
Qrng__RandomBit__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#40
	sub	sp, #40
	bl	__quantum__rt__qubit_allocate
	movs	r1, #1
	movs	r2, #0
	str	r0, [sp, #36]
	mov	r0, r1
	str	r2, [sp, #32]
	mov	r2, r1
	ldr	r3, [sp, #32]
	str	r1, [sp, #28]
	bl	__quantum__rt__array_create_1d
	str	r0, [sp, #24]
	ldr	r2, [sp, #32]
	ldr	r3, [sp, #32]
	bl	__quantum__rt__array_get_element_ptr_1d
	movw	r1, :lower16:PauliX
	movt	r1, :upper16:PauliX
	ldrb	r1, [r1]
	and	r1, r1, #3
	strb	r1, [r0]
	movs	r0, #8
	str	r0, [sp, #20]
	ldr	r2, [sp, #28]
	ldr	r3, [sp, #32]
	bl	__quantum__rt__array_create_1d
	str	r0, [sp, #16]
	ldr	r2, [sp, #32]
	ldr	r3, [sp, #32]
	bl	__quantum__rt__array_get_element_ptr_1d
	ldr	r1, [sp, #36]
	str	r1, [r0]
	ldr	r0, [sp, #24]
	ldr	r1, [sp, #16]
	bl	__quantum__qis__measure
	ldr	r1, [sp, #28]
	str	r0, [sp, #12]
	mov	r0, r1
	mov	r2, r1
	ldr	r3, [sp, #32]
	bl	__quantum__rt__array_create_1d
	str	r0, [sp, #8]
	ldr	r2, [sp, #32]
	ldr	r3, [sp, #32]
	bl	__quantum__rt__array_get_element_ptr_1d
	movw	r1, :lower16:PauliZ
	movt	r1, :upper16:PauliZ
	ldrb	r1, [r1]
	and	r1, r1, #3
	strb	r1, [r0]
	ldr	r0, [sp, #20]
	ldr	r2, [sp, #28]
	ldr	r3, [sp, #32]
	bl	__quantum__rt__array_create_1d
	str	r0, [sp, #4]
	ldr	r2, [sp, #32]
	ldr	r3, [sp, #32]
	bl	__quantum__rt__array_get_element_ptr_1d
	ldr	r1, [sp, #36]
	str	r1, [r0]
	ldr	r0, [sp, #8]
	ldr	r1, [sp, #4]
	bl	__quantum__qis__measure
	ldr	r1, [sp, #36]
	str	r0, [sp]
	mov	r0, r1
	bl	__quantum__rt__qubit_release
	ldr	r0, [sp, #24]
	bl	__quantum__rt__array_unreference
	ldr	r0, [sp, #16]
	bl	__quantum__rt__array_unreference
	ldr	r0, [sp, #8]
	bl	__quantum__rt__array_unreference
	ldr	r0, [sp, #4]
	bl	__quantum__rt__array_unreference
	ldr	r0, [sp]
	add	sp, #40
	pop	{r7, pc}
.Lfunc_end0:
	.size	Qrng__RandomBit__body, .Lfunc_end0-Qrng__RandomBit__body
	.fnend

	.globl	Qrng__RandomInt__body
	.p2align	1
	.type	Qrng__RandomInt__body,%function
	.code	16
	.thumb_func
Qrng__RandomInt__body:
	.fnstart
	.save	{r4, lr}
	push	{r4, lr}
	.pad	#32
	sub	sp, #32
	movs	r0, #0
	str	r0, [sp, #28]
	str	r0, [sp, #24]
	b	.LBB1_1
.LBB1_1:
	movs	r0, #0
	mov	r1, r0
	str	r0, [sp, #20]
	str	r1, [sp, #16]
	b	.LBB1_2
.LBB1_2:
	ldr	r0, [sp, #16]
	ldr	r1, [sp, #20]
	rsbs.w	r2, r1, #31
	mov.w	r3, #0
	sbcs	r3, r0
	str	r0, [sp, #12]
	str	r1, [sp, #8]
	blt	.LBB1_7
	b	.LBB1_3
.LBB1_3:
	bl	Qrng__RandomBit__body
	mov	r1, r0
	movw	r2, :lower16:ResultOne
	movt	r2, :upper16:ResultOne
	ldr	r2, [r2]
	str	r1, [sp, #4]
	mov	r1, r2
	bl	__quantum__rt__result_equal
	lsls	r0, r0, #31
	cmp	r0, #0
	beq	.LBB1_5
	b	.LBB1_4
.LBB1_4:
	ldr	r0, [sp, #24]
	ldr	r1, [sp, #28]
	ldr	r2, [sp, #8]
	sub.w	r3, r2, #32
	mov.w	r12, #1
	lsl.w	lr, r12, r3
	rsb.w	r4, r2, #32
	lsr.w	r4, r12, r4
	cmp	r3, #0
	it	pl
	movpl	r4, lr
	lsl.w	r12, r12, r2
	cmp	r3, #0
	it	pl
	movpl.w	r12, #0
	adds.w	r0, r0, r12
	adc.w	r1, r1, r4
	str	r0, [sp, #24]
	str	r1, [sp, #28]
	b	.LBB1_5
.LBB1_5:
	ldr	r0, [sp, #4]
	bl	__quantum__rt__result_unreference
	b	.LBB1_6
.LBB1_6:
	ldr	r0, [sp, #8]
	adds	r1, r0, #1
	ldr	r2, [sp, #12]
	adc	r3, r2, #0
	str	r1, [sp, #20]
	str	r3, [sp, #16]
	b	.LBB1_2
.LBB1_7:
	ldr	r0, [sp, #24]
	ldr	r1, [sp, #28]
	add	sp, #32
	pop	{r4, pc}
.Lfunc_end1:
	.size	Qrng__RandomInt__body, .Lfunc_end1-Qrng__RandomInt__body
	.fnend

	.globl	Qrng__RandomInts__body
	.p2align	1
	.type	Qrng__RandomInts__body,%function
	.code	16
	.thumb_func
Qrng__RandomInts__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#48
	sub	sp, #48
	movs	r0, #8
	movs	r2, #32
	movs	r3, #0
	bl	__quantum__rt__array_create_1d
	mov	r1, r0
	str	r0, [sp, #40]
	str	r1, [sp, #36]
	b	.LBB2_1
.LBB2_1:
	movs	r0, #0
	mov	r1, r0
	str	r0, [sp, #32]
	str	r1, [sp, #28]
	b	.LBB2_2
.LBB2_2:
	ldr	r0, [sp, #28]
	ldr	r1, [sp, #32]
	rsbs.w	r2, r1, #31
	mov.w	r3, #0
	sbcs	r3, r0
	str	r0, [sp, #24]
	str	r1, [sp, #20]
	blt	.LBB2_5
	b	.LBB2_3
.LBB2_3:
	ldr	r0, [sp, #40]
	bl	__quantum__rt__array_copy
	str	r0, [sp, #16]
	bl	Qrng__RandomInt__body
	ldr	r2, [sp, #16]
	str	r0, [sp, #12]
	mov	r0, r2
	ldr	r2, [sp, #20]
	ldr	r3, [sp, #24]
	str	r1, [sp, #8]
	bl	__quantum__rt__array_get_element_ptr_1d
	ldr	r1, [sp, #8]
	str	r1, [r0, #4]
	ldr	r2, [sp, #12]
	str	r2, [r0]
	ldr	r0, [sp, #16]
	str	r0, [sp, #40]
	bl	__quantum__rt__array_reference
	ldr	r0, [sp, #16]
	bl	__quantum__rt__array_unreference
	b	.LBB2_4
.LBB2_4:
	ldr	r0, [sp, #20]
	adds	r1, r0, #1
	ldr	r2, [sp, #24]
	adc	r3, r2, #0
	str	r1, [sp, #32]
	str	r3, [sp, #28]
	b	.LBB2_2
.LBB2_5:
	ldr	r0, [sp, #40]
	ldr	r1, [sp, #36]
	str	r0, [sp, #4]
	mov	r0, r1
	bl	__quantum__rt__array_unreference
	ldr	r0, [sp, #4]
	add	sp, #48
	pop	{r7, pc}
.Lfunc_end2:
	.size	Qrng__RandomInts__body, .Lfunc_end2-Qrng__RandomInts__body
	.fnend

	.globl	Microsoft__Quantum__Core__Attribute__body
	.p2align	1
	.type	Microsoft__Quantum__Core__Attribute__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Core__Attribute__body:
	.fnstart
	movs	r0, #0
	bx	lr
.Lfunc_end3:
	.size	Microsoft__Quantum__Core__Attribute__body, .Lfunc_end3-Microsoft__Quantum__Core__Attribute__body
	.fnend

	.globl	Microsoft__Quantum__Core__EntryPoint__body
	.p2align	1
	.type	Microsoft__Quantum__Core__EntryPoint__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Core__EntryPoint__body:
	.fnstart
	movs	r0, #0
	bx	lr
.Lfunc_end4:
	.size	Microsoft__Quantum__Core__EntryPoint__body, .Lfunc_end4-Microsoft__Quantum__Core__EntryPoint__body
	.fnend

	.globl	Microsoft__Quantum__Core__Inline__body
	.p2align	1
	.type	Microsoft__Quantum__Core__Inline__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Core__Inline__body:
	.fnstart
	movs	r0, #0
	bx	lr
.Lfunc_end5:
	.size	Microsoft__Quantum__Core__Inline__body, .Lfunc_end5-Microsoft__Quantum__Core__Inline__body
	.fnend

	.globl	Microsoft__Quantum__Core__Intrinsic__body
	.p2align	1
	.type	Microsoft__Quantum__Core__Intrinsic__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Core__Intrinsic__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#8
	sub	sp, #8
	movs	r1, #8
	movs	r2, #0
	str	r0, [sp, #4]
	mov	r0, r1
	mov	r1, r2
	bl	__quantum__rt__tuple_create
	ldr	r1, [sp, #4]
	str	r1, [r0, #4]
	str	r0, [sp]
	mov	r0, r1
	bl	__quantum__rt__string_reference
	ldr	r0, [sp]
	add	sp, #8
	pop	{r7, pc}
.Lfunc_end6:
	.size	Microsoft__Quantum__Core__Intrinsic__body, .Lfunc_end6-Microsoft__Quantum__Core__Intrinsic__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__CNOT__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__CNOT__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__CNOT__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	bl	__quantum__qis__cnot
	pop	{r7, pc}
.Lfunc_end7:
	.size	Microsoft__Quantum__Intrinsic__CNOT__body, .Lfunc_end7-Microsoft__Quantum__Intrinsic__CNOT__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__H__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__H__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__H__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	bl	__quantum__qis__h
	pop	{r7, pc}
.Lfunc_end8:
	.size	Microsoft__Quantum__Intrinsic__H__body, .Lfunc_end8-Microsoft__Quantum__Intrinsic__H__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__IntAsDouble__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__IntAsDouble__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__IntAsDouble__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#8
	sub	sp, #8
	mov	r2, r1
	mov	r3, r0
	str	r2, [sp, #4]
	str	r3, [sp]
	bl	__quantum__qis__intAsDouble
	add	sp, #8
	pop	{r7, pc}
.Lfunc_end9:
	.size	Microsoft__Quantum__Intrinsic__IntAsDouble__body, .Lfunc_end9-Microsoft__Quantum__Intrinsic__IntAsDouble__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__Measure__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__Measure__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__Measure__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	bl	__quantum__qis__measure
	pop	{r7, pc}
.Lfunc_end10:
	.size	Microsoft__Quantum__Intrinsic__Measure__body, .Lfunc_end10-Microsoft__Quantum__Intrinsic__Measure__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__Mz__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__Mz__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__Mz__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	bl	__quantum__qis__mz
	pop	{r7, pc}
.Lfunc_end11:
	.size	Microsoft__Quantum__Intrinsic__Mz__body, .Lfunc_end11-Microsoft__Quantum__Intrinsic__Mz__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__Rx__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__Rx__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__Rx__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#8
	sub	sp, #8
	mov	r3, r1
	mov	r12, r0
	str	r3, [sp, #4]
	str.w	r12, [sp]
	bl	__quantum__qis__rx
	add	sp, #8
	pop	{r7, pc}
.Lfunc_end12:
	.size	Microsoft__Quantum__Intrinsic__Rx__body, .Lfunc_end12-Microsoft__Quantum__Intrinsic__Rx__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__Rx__adj
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__Rx__adj,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__Rx__adj:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#8
	sub	sp, #8
	mov	r3, r1
	mov	r12, r0
	eor	r1, r1, #-2147483648
	str	r3, [sp, #4]
	str.w	r12, [sp]
	bl	__quantum__qis__rx
	add	sp, #8
	pop	{r7, pc}
.Lfunc_end13:
	.size	Microsoft__Quantum__Intrinsic__Rx__adj, .Lfunc_end13-Microsoft__Quantum__Intrinsic__Rx__adj
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__Rz__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__Rz__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__Rz__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#8
	sub	sp, #8
	mov	r3, r1
	mov	r12, r0
	str	r3, [sp, #4]
	str.w	r12, [sp]
	bl	__quantum__qis__rz
	add	sp, #8
	pop	{r7, pc}
.Lfunc_end14:
	.size	Microsoft__Quantum__Intrinsic__Rz__body, .Lfunc_end14-Microsoft__Quantum__Intrinsic__Rz__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__Rz__adj
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__Rz__adj,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__Rz__adj:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#8
	sub	sp, #8
	mov	r3, r1
	mov	r12, r0
	eor	r1, r1, #-2147483648
	str	r3, [sp, #4]
	str.w	r12, [sp]
	bl	__quantum__qis__rz
	add	sp, #8
	pop	{r7, pc}
.Lfunc_end15:
	.size	Microsoft__Quantum__Intrinsic__Rz__adj, .Lfunc_end15-Microsoft__Quantum__Intrinsic__Rz__adj
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__S__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__S__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__S__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	bl	__quantum__qis__s
	pop	{r7, pc}
.Lfunc_end16:
	.size	Microsoft__Quantum__Intrinsic__S__body, .Lfunc_end16-Microsoft__Quantum__Intrinsic__S__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__S__adj
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__S__adj,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__S__adj:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	.pad	#8
	sub	sp, #8
	str	r0, [sp, #4]
	bl	__quantum__qis__s
	ldr	r0, [sp, #4]
	bl	__quantum__qis__z
	add	sp, #8
	pop	{r7, pc}
.Lfunc_end17:
	.size	Microsoft__Quantum__Intrinsic__S__adj, .Lfunc_end17-Microsoft__Quantum__Intrinsic__S__adj
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__X__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__X__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__X__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	bl	__quantum__qis__x
	pop	{r7, pc}
.Lfunc_end18:
	.size	Microsoft__Quantum__Intrinsic__X__body, .Lfunc_end18-Microsoft__Quantum__Intrinsic__X__body
	.fnend

	.globl	Microsoft__Quantum__Intrinsic__Z__body
	.p2align	1
	.type	Microsoft__Quantum__Intrinsic__Z__body,%function
	.code	16
	.thumb_func
Microsoft__Quantum__Intrinsic__Z__body:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	bl	__quantum__qis__z
	pop	{r7, pc}
.Lfunc_end19:
	.size	Microsoft__Quantum__Intrinsic__Z__body, .Lfunc_end19-Microsoft__Quantum__Intrinsic__Z__body
	.fnend

	.globl	Qrng_RandomInts
	.p2align	1
	.type	Qrng_RandomInts,%function
	.code	16
	.thumb_func
Qrng_RandomInts:
	.fnstart
	.save	{r7, lr}
	push	{r7, lr}
	bl	Qrng__RandomInts__body
	pop	{r7, pc}
.Lfunc_end20:
	.size	Qrng_RandomInts, .Lfunc_end20-Qrng_RandomInts
	.fnend

	.type	PauliI,%object
	.section	.rodata,"a",%progbits
	.globl	PauliI
PauliI:
	.byte	0
	.size	PauliI, 1

	.type	PauliX,%object
	.globl	PauliX
PauliX:
	.byte	1
	.size	PauliX, 1

	.type	PauliY,%object
	.globl	PauliY
PauliY:
	.byte	3
	.size	PauliY, 1

	.type	PauliZ,%object
	.globl	PauliZ
PauliZ:
	.byte	2
	.size	PauliZ, 1

	.type	EmptyRange,%object
	.p2align	4
EmptyRange:
	.long	0
	.long	0
	.long	1
	.long	0
	.long	4294967295
	.long	4294967295
	.size	EmptyRange, 24

	.section	".note.GNU-stack","",%progbits
	.eabi_attribute	30, 5
