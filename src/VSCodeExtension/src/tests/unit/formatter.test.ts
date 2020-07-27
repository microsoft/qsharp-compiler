import 'mocha';
import * as assert from 'assert';
import { formatter, spaceAfterIf } from '../../formatter-core';


describe('formatter core', () => {
  it('with no rules, the code is unchanged', () => {
    const code = "using(qs=Qubit[3]){H(qs[0]);}";
    const expectedCode = "using(qs=Qubit[3]){H(qs[0]);}";

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
      const code = `H(qs[0]); if            
                       
              (1 == 1){H(qs[0]);}`;

      const expectedCode = "H(qs[0]); if (1 == 1){H(qs[0]);}";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });

    it("does not change if it is a function call", () => {
      const code = "myFunctionWithConvenientNameif(parameter)";
      const expectedCode = "myFunctionWithConvenientNameif(parameter)";

      assert.equal(formatter(code, [spaceAfterIf]), expectedCode);
    });
  });

});