all:
	cd ConstArray && make all && make clean
	cd ConstQubitArray && make all && make clean
	cd LoopRecursion && make all && make clean
	cd OptimisationIssue && make all && make clean
	cd SimpleCallable && make all && make clean
	cd SimpleLoop && make all && make clean

stats:
	python analysis.py

clean:
	cd ConstArray  && make clean
	cd ConstQubitArray  && make clean
	cd LoopRecursion  && make clean
	cd OptimisationIssue  && make clean
	cd SimpleCallable  && make clean
	cd SimpleLoop  && make clean

