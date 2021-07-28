run-expand: build-qaa build-esa analysis-example.ll
	opt -load-pass-plugin ../../Debug/libs/libQubitAllocationAnalysis.dylib \
			-load-pass-plugin ../../Debug/libs/libExpandStaticAllocation.dylib --passes="expand-static-allocation" -S analysis-example.ll	


run: build-qaa analysis-example.ll
	opt -load-pass-plugin ../../Debug/libs/libQubitAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-example.ll


build-prepare:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake ..&& popd || popd

build-qaa: build-prepare
	pushd  ../../Debug && make QubitAllocationAnalysis  && popd || popd

build-esa: build-prepare
	pushd  ../../Debug && make ExpandStaticAllocation  && popd || popd


analysis-example.ll:
	cd ConstSizeArray && make analysis-example.ll

clean:
	cd ConstSizeArray && make clean
	rm analysis-example.ll
