all: qat-qsharp

qat-qsharp: build-qat QSharpVersion/qir/Example.ll
	../../Debug/Source/Apps/qat --dump-config --apply -S --verify-module --profile baseProfile QSharpVersion/qir/Example.ll

build-qat: build-prepare
	pushd  ../../Debug && make qat  && popd || popd

build-prepare:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake ..&& popd || popd

QSharpVersion/qir/Example.ll:
	cd QSharpVersion && make qir/Example.ll

clean:
	cd QSharpVersion && make clean

