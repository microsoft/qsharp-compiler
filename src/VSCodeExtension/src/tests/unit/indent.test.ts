import "mocha";
import * as assert from "assert";
import { formatter } from "../../formatter/formatter";
import { namespaceRule } from "../../formatter/rules/indent";

describe("namespace rule", () => {
    it("has no error", () => {
        const code = "namespace Foo {";
        const expectedCode = "namespace Foo {";

        assert.equal(formatter(code, []), expectedCode);
    });

    it("trims whitespace", () => {
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

    it("matches lines with inline body", () => {
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
    
    it("matches dot-separated names", () => {
        const expectedCode = "namespace Microsoft.Quantum.Arrays { // ... }";

        let code = "     namespace Microsoft.Quantum.Arrays { // ... }";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);

        code = "namespace Microsoft.Quantum.Arrays { // ... }        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
        
        code = "namespace   Microsoft.Quantum.Arrays { // ... }        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
        
        code = "namespace Microsoft.Quantum.Arrays   { // ... }        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);

        code = "     namespace   Microsoft.Quantum.Arrays   { // ... }        ";
        assert.equal(formatter(code, [namespaceRule]), expectedCode);
    });
});
