run: build
	opt -load-pass-plugin ../../Debug/libs/libConstSizeArrayAnalysis.dylib --passes="print<const-size-array-analysis>" -disable-output analysis-problem.ll
	
build:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake .. && make ConstSizeArrayAnalysis && popd || popd
