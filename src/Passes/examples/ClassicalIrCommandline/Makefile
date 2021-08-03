emit-llvm:
	clang -O0 -fno-inline -S -emit-llvm classical-program.c -o classical-program.ll

emit-llvm-bc:
	clang -O0 -c -emit-llvm classical-program.c -o classical-program.bc


debug-ng-pass-mac: emit-llvm-bc
	opt -load-pass-plugin ../../Debug/libOpsCounter.dylib --passes="print<operation-counter>" -disable-output classical-program.bc


clean:
	rm -f classical-program.ll
	rm -f classical-program.bc

