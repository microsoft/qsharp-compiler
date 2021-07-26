emit-llvm-cpp:
	clang -O3 -S -std=c++17 -emit-llvm classical-program.cpp -o classical-program.ll

emit-llvm:
	clang -O0 -S -emit-llvm classical-program.c -o classical-program.ll

emit-llvm-bc:
	clang -O0 -c -emit-llvm classical-program.c -o classical-program.bc


debug-ng-pass-mac: emit-llvm-bc
	opt -load-pass-plugin ../../Debug/libQSharpPasses.dylib -debug --passes="operation-counter" -disable-output classical-program.bc



clean:
	rm -f classical-program.ll
	rm -f classical-program.bc