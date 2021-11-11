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
	# Run all examples and verify that the output is a valid IR
	cd QirExamples && make all

clean:
	rm -rf Release/
	rm -rf Debug/
	find . | grep ".profraw" | xargs rm	
	cd QirExamples && make clean

