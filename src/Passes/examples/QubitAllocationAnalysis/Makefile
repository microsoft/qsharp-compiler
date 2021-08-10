run-expand: build-qaa build-esa analysis-example.ll
	opt -load-pass-plugin ../../Debug/Source/Passes/libQubitAllocationAnalysis.dylib \
			-load-pass-plugin ../../Debug/Source/Passes/libExpandStaticAllocation.dylib --passes="expand-static-allocation" -S analysis-example.ll	


run: build-qaa analysis-example.ll
	opt -load-pass-plugin ../../Debug/Source/Passes/libQubitAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-example.ll

run-replace: build-ir analysis-example.ll
#	opt -loop-unroll -unroll-count=3 -unroll-allow-partial 
	opt -load-pass-plugin ../../Debug/Source/Passes/libTransformationRule.dylib --passes="mem2reg,simplifycfg,loop-simplify,loop-unroll,transformation-rule" -S analysis-example.ll > test.ll
	opt  --passes="inline" -S test.ll | opt -O1 -S


build-prepare:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake ..&& popd || popd

build-qaa: build-prepare
	pushd  ../../Debug && make QubitAllocationAnalysis  && popd || popd

build-esa: build-prepare
	pushd  ../../Debug && make ExpandStaticAllocation  && popd || popd

build-ir: build-prepare
	pushd  ../../Debug && make TransformationRule  && popd || popd


analysis-example.ll:
	cd ConstSizeArray && make analysis-example.ll

clean:
	cd ConstSizeArray && make clean
	rm analysis-example.ll
