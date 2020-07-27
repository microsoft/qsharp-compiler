export type FormatRule = (code: string) => string;

export const formatter = (code: string, rules: FormatRule[]): string => {
    return rules.reduce((formattedCode, rule) => rule(formattedCode), code);
}

export const spaceAfterIf: FormatRule = (code: string): string => {
    return code.replace(/^if[ \n]*/, "if ");
}