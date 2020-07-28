import "mocha";
import * as assert from "assert";
import { formatter, withCommentsIgnored } from "../../formatter/formatter";
import { spaceAfterIf } from "../../formatter/rules/control-structure";

describe("formatter tests", () => {
  it("has no rules", () => {
    const code = "if(1 == 1){H(qs[0]);}";
    const expectedCode = "if(1 == 1){H(qs[0]);}";

    assert.equal(formatter(code, []), expectedCode);
  });

  describe("withCommentsIgnored higher level function", () => {
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

    describe("integration tests with other rules", () => {
      it("space after if leaves comments unchanged if decorated withCommentsIgnored", () => {
        const code = '// mutable bits = new Result[0];if(1==1){Message("1==1");}';
        const expectedCode = '// mutable bits = new Result[0];if(1==1){Message("1==1");}';

        assert.equal(formatter(code, [withCommentsIgnored(spaceAfterIf)]), expectedCode);
      });

      it("leaves comment unchanged", () => {
        const code = `if\\comment a b c d
                      (a == 2) {
                        dosomething();
                      }`;

        const expectedCode = `if\\comment a b c d
                      (a == 2) {
                        dosomething();
                      }`;

        assert.equal(formatter(code, [withCommentsIgnored(spaceAfterIf)]), expectedCode);
      });
    })
  })
});
