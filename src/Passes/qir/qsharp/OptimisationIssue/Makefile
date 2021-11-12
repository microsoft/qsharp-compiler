all: qat-qsharp

qat-qsharp: QSharpVersion/qir/Example.ll
	# ${QIR_STLIB}
	${QAT_BINARY} -O3 --apply -S --verify-module --profile base QSharpVersion/qir/Example.ll  ${QIR_STLIB}  > combined.ll
	opt -O3 -S combined.ll > optimised.ll
	${QAT_BINARY} -O3 -S --apply --validate --save-validation-report validation.json --profile base optimised.ll
	


QSharpVersion/qir/Example.ll:
	cd QSharpVersion && make qir/Example.ll

clean:
	cd QSharpVersion && make clean
	rm -f combined.ll optimised.ll
	rm -f validation.json

# --code-model=<value>                                                 - Choose code model
#     =tiny                                                              -   Tiny code model
#     =small                                                             -   Small code model
#     =kernel                                                            -   Kernel code model
#     =medium                                                            -   Medium code model
#     =large                                                             -   Large code model
#   --codegen-opt-level=<uint>                                           - Override optimization level for codegen hooks
#   --cost-kind=<value>                                                  - Target cost kind
#     =throughput                                                        -   Reciprocal throughput
#     =latency                                                           -   Instruction latency
#     =code-size                                                         -   Code size

#--mem2reg                                                           - Promote Memory to Register
#      --memcpyopt                                                         - MemCpy Optimization
#      --memdep                                                            - Memory Dependence Analysis
#      --memoryssa                                                         - Memory SSA
