// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use log;

use inkwell::module::Module;
use inkwell::values::FunctionValue;

pub(crate) struct Intrinsics<'ctx> {
    pub(crate) m: FunctionValue<'ctx>,
    pub(crate) r_x: FunctionValue<'ctx>,
    pub(crate) r_y: FunctionValue<'ctx>,
    pub(crate) r_z: FunctionValue<'ctx>,
    pub(crate) reset: FunctionValue<'ctx>,
    pub(crate) h: FunctionValue<'ctx>,
    pub(crate) x: FunctionValue<'ctx>,
    pub(crate) x_ctl: FunctionValue<'ctx>,
    pub(crate) y: FunctionValue<'ctx>,
    pub(crate) y_ctl: Option<FunctionValue<'ctx>>,
    pub(crate) z: FunctionValue<'ctx>,
    pub(crate) z_ctl: FunctionValue<'ctx>,
    pub(crate) s: FunctionValue<'ctx>,
    pub(crate) s_adj: FunctionValue<'ctx>,
    pub(crate) t: FunctionValue<'ctx>,
    pub(crate) t_adj: FunctionValue<'ctx>,
    pub(crate) dumpmachine: FunctionValue<'ctx>,
}

impl<'ctx> Intrinsics<'ctx> {
    pub fn new(module: &Module<'ctx>) -> Self {
        Intrinsics {
            m: Intrinsics::get_intrinsic_function_body(module, "M")
                .expect("M gate must be defined"),
            r_x: Intrinsics::get_intrinsic_function_body(module, "Rx")
                .expect("Rx gate must be defined"),
            r_y: Intrinsics::get_intrinsic_function_body(module, "Ry")
                .expect("Ry gate must be defined"),
            r_z: Intrinsics::get_intrinsic_function_body(module, "Rz")
                .expect("Rz gate must be defined"),
            reset: Intrinsics::get_intrinsic_function_body(module, "Reset")
                .expect("Reset must be defined"),
            h: Intrinsics::get_intrinsic_function_body(module, "H")
                .expect("H gate must be defined"),
            x: Intrinsics::get_intrinsic_function_body(module, "X")
                .expect("X gate must be defined"),
            x_ctl: Intrinsics::get_intrinsic_function_ctl(module, "X")
                .expect("X_ctl gate must be defined"),
            y: Intrinsics::get_intrinsic_function_body(module, "Y")
                .expect("Y gate must be defined"),
            y_ctl: Intrinsics::get_intrinsic_function_ctl(module, "Y"),
            z: Intrinsics::get_intrinsic_function_body(module, "Z")
                .expect("Z gate must be defined"),
            z_ctl: Intrinsics::get_intrinsic_function_ctl(module, "Z")
                .expect("Z_ctl gate must be defined"),
            s: Intrinsics::get_intrinsic_function_body(module, "S")
                .expect("S gate must be defined"),
            s_adj: Intrinsics::get_intrinsic_function_adj(module, "S")
                .expect("S_adj gate must be defined"),
            t: Intrinsics::get_intrinsic_function_body(module, "T")
                .expect("T gate must be defined"),
            t_adj: Intrinsics::get_intrinsic_function_adj(module, "T")
                .expect("T_adj gate must be defined"),
            dumpmachine: Intrinsics::get_qis_intrinsic_function_body(module, "dumpmachine")
                .expect("dumpmachine must be defined"),
        }
    }

    fn get_qis_intrinsic_function_body(
        module: &Module<'ctx>,
        name: &str,
    ) -> Option<FunctionValue<'ctx>> {
        let function_name = format!("__quantum__qis__{}__body", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_intrinsic_function_body(
        module: &Module<'ctx>,
        name: &str,
    ) -> Option<FunctionValue<'ctx>> {
        let function_name = format!("Microsoft__Quantum__Intrinsic__{}__body", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_intrinsic_function_ctl(
        module: &Module<'ctx>,
        name: &str,
    ) -> Option<FunctionValue<'ctx>> {
        let function_name = format!("Microsoft__Quantum__Intrinsic__{}__ctl", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_intrinsic_function_adj(
        module: &Module<'ctx>,
        name: &str,
    ) -> Option<FunctionValue<'ctx>> {
        let function_name = format!("Microsoft__Quantum__Intrinsic__{}__adj", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_function(module: &Module<'ctx>, function_name: &str) -> Option<FunctionValue<'ctx>> {
        let defined_function = module.get_function(&function_name);
        match defined_function {
            None => {
                log::debug!(
                    "{} global function was not defined in the module",
                    function_name
                );
                None
            }
            Some(value) => Some(value),
        }
    }
}

#[cfg(test)]
mod tests {
    use crate::emit::Context;

    use super::*;

    #[test]
    fn intrinsics_can_be_loaded() {
        let ctx = inkwell::context::Context::create();
        let name = "temp";
        let context = Context::new(&ctx, name);
        let _ = Intrinsics::new(&context.module);
    }
}
