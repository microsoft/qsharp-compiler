import "mocha";
import * as assert from "assert";
import { formatter } from "../../formatter/formatter";
import { argsRule } from "../../formatter/rules/declaration";

describe("arguments rule", () => {
    it("has no error", () => {
        const code = "operation Foo (q : Qubit, n : Int) : Bool";
        const expectedCode = "operation Foo (q : Qubit, n : Int) : Bool";

        assert.equal(formatter(code, [argsRule]), expectedCode);
    });

    it("keeps leading whitespace", () => {
        const code = "  operation Foo (q : Qubit, n : Int) : Bool";
        const expectedCode = "  operation Foo (q : Qubit, n : Int) : Bool";

        assert.equal(formatter(code, [argsRule]), expectedCode);
    });

    it("removes unnecessary spaces (non-arguments)", () => {
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

    it("removes unnecessary spaces (arguments)", () => {
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

    it("matches functions", () => {
        const code = "  function   Foo   (q  :Qubit,   n :  Int)   :  Bool";
        const expectedCode = "  function Foo (q : Qubit, n : Int) : Bool";

        assert.equal(formatter(code, [argsRule]), expectedCode);
    });
});
