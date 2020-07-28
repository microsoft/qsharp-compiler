import "mocha";
import * as assert from "assert";
import { formatter } from "../../formatter/formatter";

describe("formatter tests", () => {
  it("with no rules, the code is unchanged", () => {
    const code = "if(1 == 1){H(qs[0]);}";
    const expectedCode = "if(1 == 1){H(qs[0]);}";

    assert.equal(formatter(code, []), expectedCode);
  });
});
