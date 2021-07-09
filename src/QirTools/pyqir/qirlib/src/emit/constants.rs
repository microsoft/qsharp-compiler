// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use super::types::Types;
use inkwell::module::Module;
use inkwell::values::GlobalValue;
use inkwell::values::PointerValue;

pub(crate) struct Constants<'ctx> {
    pub(crate) unit: PointerValue<'ctx>,
    pub(crate) pauli_i: GlobalValue<'ctx>,
    pub(crate) pauli_x: GlobalValue<'ctx>,
    pub(crate) pauli_y: GlobalValue<'ctx>,
    pub(crate) pauli_z: GlobalValue<'ctx>,
    pub(crate) empty_range: GlobalValue<'ctx>,
}

impl<'ctx> Constants<'ctx> {
    pub fn new(module: &Module<'ctx>, types: &Types<'ctx>) -> Self {
        Constants {
            unit: types.tuple.const_null(),
            pauli_i: Constants::get_global(module, "PauliI"),
            pauli_x: Constants::get_global(module, "PauliX"),
            pauli_y: Constants::get_global(module, "PauliY"),
            pauli_z: Constants::get_global(module, "PauliZ"),
            empty_range: Constants::get_global(module, "EmptyRange"),
        }
    }

    fn get_global(module: &Module<'ctx>, name: &str) -> GlobalValue<'ctx> {
        if let Some(defined_global) = module.get_global(name) {
            return defined_global;
        }
        panic!("{} global constant was not defined in the module", name);
    }
}

#[cfg(test)]
mod tests {
    use crate::emit::Context;

    use super::*;

    #[test]
    fn constants_can_be_loaded() {
        let ctx = inkwell::context::Context::create();
        let name = "temp";
        let context = Context::new(&ctx, name);
        let types = Types::new(&context.context, &context.module);
        let _ = Constants::new(&context.module, &types);
    }
}
