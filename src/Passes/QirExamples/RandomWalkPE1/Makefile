all: qat-qsharp

qat-qsharp: build-qat QSharpVersion/qir/Example.ll
	../../Debug/Source/Apps/qat --apply -O3 -S --verify-module  --profile baseProfile QSharpVersion/qir/Example.ll > optimised.ll

build-qat: build-prepare
	pushd  ../../Debug && make qat  && popd || popd

build-prepare:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake ..&& popd || popd

QSharpVersion/qir/Example.ll:
	cd QSharpVersion && make qir/Example.ll

clean:
	cd QSharpVersion && make clean
	rm -f optimised.ll
