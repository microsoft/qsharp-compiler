// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use log;

use super::types::Types;
use inkwell::module::Module;
use inkwell::values::GlobalValue;
use inkwell::values::PointerValue;

pub(crate) struct Constants<'ctx> {
    pub(crate) unit: Option<PointerValue<'ctx>>,
    pub(crate) pauli_i: Option<GlobalValue<'ctx>>,
    pub(crate) pauli_x: GlobalValue<'ctx>,
    pub(crate) pauli_y: GlobalValue<'ctx>,
    pub(crate) pauli_z: GlobalValue<'ctx>,
    pub(crate) empty_range: Option<GlobalValue<'ctx>>,
}

impl<'ctx> Constants<'ctx> {
    pub fn new(module: &Module<'ctx>, types: &Types<'ctx>) -> Self {
        Constants {
            unit: types.tuple.map_or_else(|| None, |t| Some(t.const_null())),
            pauli_i: Constants::get_global(module, "PauliI"),
            pauli_x: Constants::get_global(module, "PauliX").expect("PauliX must be defined"),
            pauli_y: Constants::get_global(module, "PauliY").expect("PauliY must be defined"),
            pauli_z: Constants::get_global(module, "PauliZ").expect("PauliZ must be defined"),
            empty_range: Constants::get_global(module, "EmptyRange"),
        }
    }

    fn get_global(module: &Module<'ctx>, name: &str) -> Option<GlobalValue<'ctx>> {
        let defined_global = module.get_global(name);
        match defined_global {
            None => {
                log::debug!("{} global constant was not defined in the module", name);
                None
            }
            Some(value) => Some(value),
        }
    }
}

#[cfg(test)]
mod tests {
    use crate::emit::{Context, ContextType};

    use super::*;

    #[test]
    fn constants_can_be_loaded() {
        let ctx = inkwell::context::Context::create();
        let name = String::from("temp");
        let context = Context::new(&ctx, ContextType::Template(&name)).unwrap();
        let types = Types::new(&context.context, &context.module);
        let _ = Constants::new(&context.module, &types);
    }
}
