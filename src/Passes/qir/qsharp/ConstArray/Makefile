all: QSharpVersion/qir/Example.ll
	${QAT_BINARY}  QSharpVersion/qir/Example.ll ${QIR_STLIB} -O3 -S --apply > combined.ll 
	opt -O3 -S combined.ll > optimised.ll
	${QAT_BINARY} -O3 -S --apply --validate --save-validation-report validation.json --profile base optimised.ll > /dev/null
	@echo "Validation for ConstArray finished"
	

QSharpVersion/qir/Example.ll:
	cd QSharpVersion && make qir/Example.ll

clean:
	cd QSharpVersion && make clean
	rm -f *.ll
	rm -f validation.json