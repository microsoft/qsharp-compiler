run: build
	opt -load-pass-plugin ../../Debug/libs/libQubitAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-problem-4.ll

build:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake .. && make QubitAllocationAnalysis && popd || popd

