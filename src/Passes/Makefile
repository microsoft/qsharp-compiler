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

clean:
	rm -rf Release/
	rm -rf Debug/

