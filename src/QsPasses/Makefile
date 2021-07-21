stylecheck:
	@source develop.env && python -m TasksCI stylecheck

fixstyle:
	@source develop.env && python -m TasksCI --loglevel warning stylecheck --fix-issues 

lint:
	@source develop.env && python -m TasksCI lint

tests:
	@source develop.env && python -m TasksCI test


clean:
	rm -rf Release/
	rm -rf Debug/

runci: stylecheck lint tests clean
