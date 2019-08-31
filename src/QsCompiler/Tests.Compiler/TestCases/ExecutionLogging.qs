// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/// This namespace contains test cases for tests based on logging during execution
namespace Microsoft.Quantum.Testing.ExecutionLogging {

	operation ULog<'T> (i : 'T) : Unit {
		body (...) {
			Message($"{i}");
		}
		adjoint (...) {
			Message($"Adjoint {i}");
		}
		controlled (cs, ...) {
			Message($"Controlled {i}");
		}
		controlled adjoint (cs, ...) {
			Message($"Controlled Adjoint {i}");
		}
	}


	// tests related to auto-generation of functor specializations for operations involving conjugations

	operation SpecGenForConjugations () : Unit 
	is Adj + Ctl {

		within {
			ULog("U1");
			ULog("V1");

			within {
				ULog("U3");
				ULog("V3");
			}
			apply {
				ULog("Core3");
			}
		}
		apply {
			ULog("Core1");

			within {
				ULog("U2");
				ULog("V2");
			}
			apply {
				ULog("Core2");
			}
		}
	}

	operation ConjugationsInBody () : Unit {
		SpecGenForConjugations();
	}

	operation ConjugationsInAdjoint () : Unit {
		Adjoint SpecGenForConjugations();
	}

	operation ConjugationsInControlled () : Unit {
		Controlled SpecGenForConjugations(new Qubit[0], ());
	}

	operation ConjugationsInControlledAdjoint () : Unit {
		Controlled Adjoint SpecGenForConjugations(new Qubit[0], ());
	}
}