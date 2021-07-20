stylecheck:
	@python scripts/FormatSource/__init__.py

lint:
	@echo "Static analysis not added yet"

tests:
	mkdir Debug
	cd Debug && cmake .. && make -j4 && ctest

clean:
	rm -rf Release/
	rm -rf Debug/
