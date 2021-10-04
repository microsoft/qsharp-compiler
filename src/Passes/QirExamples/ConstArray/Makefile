link: QSharpVersion/qir/Example.ll CppRuntime/runtime.ll build-qat
	../../Debug/Source/Apps/qat  QSharpVersion/qir/Example.ll CppRuntime/runtime.ll -S --apply --assume-no-except --no-transform-execution-path-only > combined.ll 
	opt -O3 -S combined.ll > optimised.ll

qat-qsharp: build-qat QSharpVersion/qir/Example.ll
	../../Debug/Source/Apps/qat --load ../../Debug/ComponentExamples/libCArrayMap.dylib -S --always-inline --apply --disable-transformation-rules QSharpVersion/qir/Example.ll

build-qat: build-prepare
	pushd  ../../Debug && make qat   && make CArrayMap  && popd || popd

build-prepare:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake ..&& popd || popd

QSharpVersion/qir/Example.ll:
	cd QSharpVersion && make qir/Example.ll

clean:
	cd QSharpVersion && make clean
	rm -f *.ll
