all:
	clang++ -O0 -S -emit-llvm -c runtime.cpp -o runtime.ll
	clang -O0 -S -emit-llvm  -c altruntime.c -o runtime2.ll

xx:
	clang++ -O1 -mllvm -disable-llvm-optzns -S -emit-llvm -c runtime.cpp -o runtime.ll
	clang -O1 -mllvm -disable-llvm-optzns -S -emit-llvm  -c altruntime.c -o runtime2.ll

clean:
	rm -f runtime.ll
	rm -f runtime2.ll	