run: build
#	opt -load-pass-plugin ../../Debug/libs/libMetaData.dylib --passes="meta-data" -S analysis-problem-4.ll
	opt -load-pass-plugin ../../Debug/libs/libConstSizeArrayAnalysis.dylib --passes="print<const-size-array-analysis>" -disable-output analysis-problem-4.ll

build:
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake .. && make ConstSizeArrayAnalysis && popd || popd
	pushd  ../../ && mkdir -p Debug && cd Debug && cmake .. && make MetaData && popd || popd
