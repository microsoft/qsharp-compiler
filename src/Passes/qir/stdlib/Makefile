lib/stdlib.ll:
	clang -O0 -mllvm -disable-llvm-optzns -S -emit-llvm  -c src/stdlib.c -o lib/stdlib.ll

target/stdlib.bc: lib/stdlib.ll
	llvm-as lib/stdlib.ll -o target/stdlib.bc

target/stdlib.o: target/stdlib.bc
	mkdir -p target/
	llc -filetype=obj -o target/stdlib.o target/stdlib.bc 

target/c-test-suite: target/stdlib.o
	clang -O1 target/stdlib.o tests/csuite/test.c -o target/c-test-suite

target/ir-test-suite: target/stdlib.ll
	llvm-link tests/irsuite/test.ll lib/stdlib.ll  -o target/ir-test-suite.bc
	llc -filetype=obj -o target/ir-test-suite.o target/ir-test-suite.bc	
	clang -O1 target/ir-test-suite.o -o target/ir-test-suite

test: target/c-test-suite target/ir-test-suite

clean:
	rm -rf target