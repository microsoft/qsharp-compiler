inject: QSharpVersion/qir/Example.ll CppRuntime/runtime.ll
	python strip.py CppRuntime/runtime.ll stripped.ll
	llvm-link  stripped.ll QSharpVersion/qir/Example.ll -o combined.ll	
	opt -O3 -S combined.ll > optimised.ll

CppRuntime/runtime.ll:
	cd CppRuntime && make	

qat-qsharp: build-qat QSharpVersion/qir/Example.ll
	../../Debug/Source/Apps/qat --load ../../Debug/ComponentExamples/libCArrayMap.dylib -S --always-inline --apply --disable-transformation-rules QSharpVersion/qir/Example.ll

build-qat: build-prepare
	pushd  ../../Debug && make qat   && make libCArrayMap  && popd || popd

build-prepare:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake ..&& popd || popd

QSharpVersion/qir/Example.ll:
	cd QSharpVersion && make qir/Example.ll

clean:
	cd QSharpVersion && make clean
	cd CppRuntime && make clean
	rm *.ll
