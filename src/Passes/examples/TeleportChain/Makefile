all: qat-qsharp

qat-qsharp: build-qat QSharpVersion/qir/Example.ll
	../../Debug/Source/Apps/qat --generate -S --verify-module --always-inline --profile baseProfile QSharpVersion/qir/Example.ll

build-qat: build-prepare
	pushd  ../../Debug && make qat  && popd || popd

build-prepare:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake ..&& popd || popd

QSharpVersion/qir/Example.ll:
	cd QSharpVersion && make qir/Example.ll

clean:
	cd QSharpVersion && make clean


# --no-disable-reference-counting --no-disable-reference-counting --no-disable-alias-counting --no-disable-string-support --no-optimise-branch-quatum-one --no-optimise-branch-quatum-zero --no-use-static-qubit-array-allocation --no-use-static-qubit-allocation --no-use-static-result-allocation