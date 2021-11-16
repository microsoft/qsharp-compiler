nothing:
	@echo "Preventing the user from accidentality running the first command."

documentation:
	docker build -f Docker/Docs.dockerfile -t qir-passes-docs:latest .	

documentation-shell: documentation
	docker run --rm -i -t qir-passes-docs:latest sh

serve-docs:  documentation
	docker run -it --rm -p 8080:80 --name qir-documentation -t qir-passes-docs:latest

doxygen:
	doxygen doxygen.cfg


linux-docker:
	docker build --no-cache -f Docker/CI.Ubuntu20.dockerfile -t qir-passes-ubuntu:latest .

linux-ci: linux-docker
	docker run -it --rm -t qir-passes-ubuntu:latest ./manage runci

test-examples:
	mkdir -p Debug
	cd Debug && cmake .. && make qat
	cd qir/stdlib && make
	export QAT_BINARY=${PWD}/Debug/qir/qat/Apps/qat && \
		export QIR_STLIB=${PWD}/qir/stdlib/lib/stdlib.ll && \
		cd qir/qsharp && \
		make


clean:
	rm -rf Release/
	rm -rf Debug/
	find . | grep ".profraw" | xargs rm	
	cd QirExamples && make clean

