nothing:
	@echo "Preventing the user from accidently running the clean command."

documentation:
	docker build -f Docker/Docs.dockerfile -t qir-passes-docs:latest .	

documentation-shell: documentation
	docker run --rm -i -t qir-passes-docs:latest sh

serve-docs:  documentation
	docker run -it --rm -p 8080:80 --name qir-documentation -t qir-passes-docs:latest

doxygen:
	doxygen doxygen.cfg


test-examples:
	# Run all examples and verify that the output is a valid IR
	cd examples && make all

clean:
	rm -rf Release/
	rm -rf Debug/
	find . | grep ".profraw" | xargs rm	
	cd examples && make clean

