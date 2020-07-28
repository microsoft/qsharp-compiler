import "mocha";
import * as assert from "assert";
import { formatter, withCommentsIgnored } from "../../formatter/formatter";

describe("formatter tests", () => {
  it("has no rules", () => {
    const code = "if(1 == 1){H(qs[0]);}";
    const expectedCode = "if(1 == 1){H(qs[0]);}";

    assert.equal(formatter(code, []), expectedCode);
  });

  it("ignoring comments high level function", () => {
    const code =
      `part1 = 1;
// comment here
part2 = 2;`;

    const ruleCalledWith: string[] = [];

    const mockRule = (ruleCode: string) => {
      ruleCalledWith.push(ruleCode);
      return ruleCode;
    }

    const decoratedRule = withCommentsIgnored(mockRule);
    const formattedCode = formatter(code, [decoratedRule]);

    assert.equal(ruleCalledWith.length, 2);
    assert.equal(ruleCalledWith[0], "part1 = 1;\n");
    assert.equal(ruleCalledWith[1], "\npart2 = 2;");

    assert.equal(formattedCode, code);
  });
});
