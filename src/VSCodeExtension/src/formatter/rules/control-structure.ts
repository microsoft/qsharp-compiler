import { FormatRule } from "../formatter";

/**
 * Leaves just one space after an if statement.
 *
 * @param code input string
 *
 * @example `if                              (1 == 1){H(qs[0]);}` ->
 * `if (1 == 1){H(qs[0]);}`
 */
export const spaceAfterIf: FormatRule = (code: string): string => {
    return code.replace(/(^| +|;|})if[ \n]*\(/, "$1if (");
};

export default [spaceAfterIf];
