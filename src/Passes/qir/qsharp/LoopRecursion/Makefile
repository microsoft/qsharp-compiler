all: qat-qsharp

qat-qsharp: QSharpVersion/qir/Example.ll
	${QAT_BINARY} -O3 --apply -S --verify-module  --profile base QSharpVersion/qir/Example.ll > combined.ll
	opt -O3 -S combined.ll > optimised.ll
	${QAT_BINARY} -O3 -S --apply --validate --save-validation-report validation.json --profile base optimised.ll > /dev/null
	@echo "Validation for LoopRecursion finished"
	
	

QSharpVersion/qir/Example.ll:
	cd QSharpVersion && make qir/Example.ll

clean:
	cd QSharpVersion && make clean
	rm -f combined.ll optimised.ll
	rm -f validation.json