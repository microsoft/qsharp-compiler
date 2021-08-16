run-expand: build-qaa build-esa analysis-example.ll
	opt -load-pass-plugin ../../Debug/Source/Passes/libQirAllocationAnalysis.dylib \
			-load-pass-plugin ../../Debug/Source/Passes/libExpandStaticAllocation.dylib --passes="expand-static-allocation" -S analysis-example.ll	


run: build-qaa analysis-example.ll
	# TODO(tfr): Add comments
	opt -load-pass-plugin ../../Debug/Source/Passes/libQirAllocationAnalysis.dylib --passes="print<qubit-allocation-analysis>" -disable-output analysis-example.ll

run-replace: build-ir build-qaa build-esa analysis-example.ll
#	opt -loop-unroll -unroll-count=3 -unroll-allow-partial 
	opt -load-pass-plugin ../../Debug/Source/Passes/libQirAllocationAnalysis.dylib \
			-load-pass-plugin ../../Debug/Source/Passes/libExpandStaticAllocation.dylib --passes="expand-static-allocation" -S analysis-example.ll > analysis-example-step1.ll
	opt -load-pass-plugin ../../Debug/Source/Passes/libTransformationRule.dylib --passes="loop-simplify,loop-unroll,restrict-qir<base-profile>" -S analysis-example-step1.ll > analysis-example-final.ll
	opt  --passes="inline" -S test2.ll | opt -O1 -S


build-prepare:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake ..&& popd || popd

build-qaa: build-prepare
	pushd  ../../Debug && make QirAllocationAnalysis  && popd || popd

build-esa: build-prepare
	pushd  ../../Debug && make ExpandStaticAllocation  && popd || popd

build-ir: build-prepare
	pushd  ../../Debug && make TransformationRule  && popd || popd


analysis-example.ll:
	cd TeleportChain && make analysis-example.ll

clean:
	cd TeleportChain && make clean
	rm analysis-example.ll
	rm analysis-example-step1.ll
	rm analysis-example-final.ll
