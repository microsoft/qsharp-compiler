import "mocha";
import * as assert from "assert";
import { formatter } from "../../formatter/formatter";
import { spaceAfterIf } from "../../formatter/rules/control-structure";

describe("control structure formatter tests", () => {
  it("with no rules, the code is unchanged", () => {
    const code = "if(1 == 1){H(qs[0]);}";
    const expectedCode = "if(1 == 1){H(qs[0]);}";

    assert.equal(formatter(code, []), expectedCode);
  });

  describe("space after if rule", () => {
    it("adds space after if statement", () => {
      const code = "if(1 == 1){H(qs[0]);}";
      const expectedCode = "if (1 == 1){H(qs[0]);}";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("does not add space if it is already there", () => {
      const code = "if (1 == 1){H(qs[0]);}";
      const expectedCode = "if (1 == 1){H(qs[0]);}";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("replaces an if with several spaces with just one", () => {
      const code = "if                              (1 == 1){H(qs[0]);}";
      const expectedCode = "if (1 == 1){H(qs[0]);}";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("changes breaklines and whitespace with just one space", () => {
      const code = `if            
                       
              (1 == 1){H(qs[0]);}`;

      const expectedCode = "if (1 == 1){H(qs[0]);}";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("changes breaklines and whitespace if there is preceeding code", () => {
      const code = `H(qs[0]);   if            
                       
              (1 == 1){H(qs[0]);}`;

      const expectedCode = "H(qs[0]);   if (1 == 1){H(qs[0]);}";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("changes breaklines and whitespace if there is preceeding code and no space after", () => {
      const code = `H(qs[0]);   if(1 == 1){H(qs[0]);}`;
      const expectedCode = "H(qs[0]);   if (1 == 1){H(qs[0]);}";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("does not change if it is a function call", () => {
      const code = "myFunctionWithConvenientNameif(parameter)";
      const expectedCode = "myFunctionWithConvenientNameif(parameter)";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("does not add any space if there is an identifier with if in the name", () => {
      const code = "functionWithifInTheMiddle(parameter)";
      const expectedCode = "functionWithifInTheMiddle(parameter)";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("formats space after if it is preceeding with a semicolon", () => {
      const code = 'mutable bits = new Result[0];if(1==1){Message("1==1");}';
      const expectedCode =
        'mutable bits = new Result[0];if (1==1){Message("1==1");}';

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("formats space after if it is preceeding with a semicolon with line break", () => {
      const code = `mutable bits = new Result[0];if 
                   (1==1){Message(\"1==1\");}`;

      const expectedCode =
        'mutable bits = new Result[0];if (1==1){Message("1==1");}';

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("formats space after if it is precceding with a }", () => {
      const code = `for (idxBit in 1..BitSizeI(max)) {
                      set bits += [SampleQuantumRandomNumberGenerator()];
                    }if(2==2){Message("2==2");}`;

      const expectedCode = `for (idxBit in 1..BitSizeI(max)) {
                      set bits += [SampleQuantumRandomNumberGenerator()];
                    }if (2==2){Message("2==2");}`;

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });
  });
});
