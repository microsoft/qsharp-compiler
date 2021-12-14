// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as assert from 'assert';
import { sanitizeNamespaceName } from '../src/sanitize-namespace-name';

console.log();
console.log("--------------------------------");
console.log("  VS Code Extension Unit Tests");
console.log("--------------------------------");

describe('Testing the "sanitizeNamespaceName" function', function() {
	let testCases = [
		// Make sure it doesn't change names that are already valid
		["aaabbbcccddd",    "aaabbbcccddd"],
		["AaaBbbCccDdd",    "AaaBbbCccDdd"],
		["aaabbbccc123", "aaabbbccc123"],
		["aaa.bbb.ccc",  "aaa.bbb.ccc"],
		["aaa_bbb_ccc",  "aaa_bbb_ccc"],

		// Try combinations of hyphens
		["aaa-bbb-ccc",     "aaa_bbb_ccc"],
		["aaa--bbb--ccc",   "aaa_bbb_ccc"],
		["aaa---bbb---ccc", "aaa_bbb_ccc"],

		// Try combinations of spaces
		["aaa bbb ccc",     "aaa_bbb_ccc"],
		["aaa  bbb  ccc",   "aaa_bbb_ccc"],
		["aaa   bbb   ccc", "aaa_bbb_ccc"],

		// Try mixed invalid characters
		["aaa 'bbb', ccc!", "aaa_bbb_ccc"],

		// Try invalid underscore and dot combinations
		["_aaabbbccc_",     "_aaabbbccc"],
		["__aaabbbccc__",   "_aaabbbccc"],
		["___aaabbbccc___", "_aaabbbccc"],
		["aaa..bbb..ccc",   "aaa.bbb.ccc"],
		["aaa...bbb...ccc", "aaa.bbb.ccc"],
		["aa._bb_.cc._.dd", "aa._bb.cc.dd"]
	];

	testCases.forEach(function(element) {
		let projectName = element[0];
		let expectedNamespaceName = element[1];
		let namespaceName = sanitizeNamespaceName(projectName);

	    it(projectName + '	returns ' + expectedNamespaceName, function() {
		assert.equal(namespaceName, expectedNamespaceName);
		})
	})
});
