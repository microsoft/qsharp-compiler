analysis-example.ll:
	dotnet build ConstSizeArray.csproj 
	opt -S qir/ConstSizeArray.ll -O1  > ../analysis-example.ll  
	make clean 

comparison:
	clang++ -S -emit-llvm -std=c++14 -stdlib=libc++ Comparison.cpp -o comparison.ll

clean:
	rm -rf bin
	rm -rf obj
	rm -rf qir
	