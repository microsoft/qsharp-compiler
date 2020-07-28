import "mocha";
import * as assert from "assert";
import { formatter } from "../../formatter/formatter";
import { argsRule } from "../../formatter/rules/declaration";

describe("arguments rule", () => {
    it("no error", () => {
        const code = "operation Foo (q : Qubit, n : Int) : Bool";
        const expectedCode = "operation Foo (q : Qubit, n : Int) : Bool";

        assert.equal(formatter(code, [argsRule]), expectedCode);
    });

    it("keep leading whitespace", () => {
        const code = "  operation Foo (q : Qubit, n : Int) : Bool";
        const expectedCode = "  operation Foo (q : Qubit, n : Int) : Bool";

        assert.equal(formatter(code, [argsRule]), expectedCode);
    });

    it("remove unnecessary spaces (non-arguments)", () => {
        const expectedCode = "operation Foo (q : Qubit, n : Int) : Bool";

        let code = "operation   Foo (q : Qubit, n : Int) : Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);

        code = "operation Foo   (q : Qubit, n : Int) : Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);

        code = "operation Foo (q : Qubit, n : Int)   : Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);

        code = "operation Foo (q : Qubit, n : Int) :   Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);
    });

    it("remove unnecessary spaces (arguments)", () => {
        const expectedCode = "operation Foo (q : Qubit, n : Int) : Bool";

        let code = "operation Foo (q   : Qubit, n : Int) : Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);

        code = "operation Foo (q :    Qubit, n : Int) : Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);

        code = "operation Foo (q : Qubit, n   : Int) : Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);

        code = "operation Foo (q : Qubit, n :   Int) : Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);

        code = "operation Foo (q : Qubit,   n : Int) : Bool";
        assert.equal(formatter(code, [argsRule]), expectedCode);
    });

    it("match functions", () => {
        const code = "  function   Foo   (q  :Qubit,   n :  Int)   :  Bool";
        const expectedCode = "  function Foo (q : Qubit, n : Int) : Bool";

        assert.equal(formatter(code, [argsRule]), expectedCode);
    });
});
