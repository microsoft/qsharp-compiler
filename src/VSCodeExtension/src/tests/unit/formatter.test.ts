import "mocha";
import * as assert from "assert";
import { formatter, withCommentsIgnored } from "../../formatter/formatter";

describe("formatter tests", () => {
  it("has no rules", () => {
    const code = "if(1 == 1){H(qs[0]);}";
    const expectedCode = "if(1 == 1){H(qs[0]);}";

    assert.equal(formatter(code, []), expectedCode);
  });

  describe("withCommentsIgnored higher level function", () => {
    let ruleCalledWith: string[];

    const mockRule = (ruleCode: string) => {
      ruleCalledWith.push(ruleCode);
      return ruleCode;
    }

    const decoratedRule = withCommentsIgnored(mockRule);

    beforeEach(() => {
      ruleCalledWith = [];
    })

    it("ignoring comments high level function", () => {
      const code =
        `part1 = 1;
// comment here
part2 = 2;`;

      const formattedCode = formatter(code, [decoratedRule]);

      assert.equal(ruleCalledWith.length, 2);
      assert.equal(ruleCalledWith[0], "part1 = 1;\n");
      assert.equal(ruleCalledWith[1], "\npart2 = 2;");

      assert.equal(formattedCode, code);
    });

    it("multiple comments, multiple line comments, calls the rule with the non-comment code", () => {
      const code =
        `
// This is a test comment
namespace Qrng {
  operation SampleQuantumRandomNumberGenerator() : Result {
    using (q = Qubit())  {  // Allocate a qubit.
      H(q);               // Put the qubit to superposition. It now has a 50% chance of being 0 or 1.
      return MResetZ(q);  // Measure the qubit value.
    }
  }
}
`;
      const formattedCode = formatter(code, [decoratedRule]);

      assert.equal(ruleCalledWith.length, 5);
      assert.equal(ruleCalledWith[0], "\n");
      assert.equal(ruleCalledWith[1], "\nnamespace Qrng {\n  operation SampleQuantumRandomNumberGenerator() : Result {\n    using (q = Qubit())  {  ");
      assert.equal(ruleCalledWith[2], "\n      H(q);               ");
      assert.equal(ruleCalledWith[3], "\n      return MResetZ(q);  ");
      assert.equal(ruleCalledWith[4], "\n    }\n  }\n}\n");

      assert.equal(formattedCode, code);
    });
  });
});
