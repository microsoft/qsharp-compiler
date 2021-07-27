run: build analysis-example.ll
	opt -load-pass-plugin ../../Debug/libs/libQubitAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-example.ll

build:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake .. && make QubitAllocationAnalysis && popd || popd


analysis-example.ll:
	cd ConstSizeArray && make analysis-example.ll

clean:
	cd ConstSizeArray && make clean
	rm analysis-example.ll