import "mocha";
import * as assert from "assert";
import { formatter } from "../../formatter/formatter";
import { namespaceRule } from "../../formatter/rules/indent";

describe("namespace rule", () => {
    it("no error", () => {
        const code = "namespace Foo {";
        const expectedCode = "namespace Foo {";

        assert.equal(formatter(code, []), expectedCode);
    });

    it("trim whitespace", () => {
        const expectedCode = "namespace Foo {";

        let code = "     namespace Foo {";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);

        code = "namespace Foo {        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
        
        code = "namespace   Foo {        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
        
        code = "namespace Foo   {        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);

        code = "     namespace   Foo   {        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
    });

    it("match lines with inline body", () => {
        const expectedCode = "namespace Foo { // ... }";

        let code = "     namespace Foo { // ... }";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);

        code = "namespace Foo { // ... }        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
        
        code = "namespace   Foo { // ... }        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
        
        code = "namespace Foo   { // ... }        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);

        code = "     namespace   Foo   { // ... }        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
    });
});
