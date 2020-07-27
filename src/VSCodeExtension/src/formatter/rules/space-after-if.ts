import { FormatRule } from "../formatter";

export const spaceAfterIf: FormatRule = (code: string): string => {
  return code.replace(/(^| +|;|})if[ \n]*/, "$1if ");
};
