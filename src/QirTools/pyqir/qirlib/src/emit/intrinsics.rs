// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use inkwell::module::Module;
use inkwell::values::FunctionValue;

pub const M: &str = "M";
pub const R_X: &str = "Rx";
pub const R_Y: &str = "Ry";
pub const R_Z: &str = "Rz";
pub const RESET: &str = "Reset";

pub const H: &str = "H";

pub const X: &str = "X";
pub const Y: &str = "Y";
pub const Z: &str = "Z";

pub const S: &str = "S";
pub const T: &str = "T";

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
    pub(crate) y_ctl: FunctionValue<'ctx>,
    pub(crate) z: FunctionValue<'ctx>,
    pub(crate) z_ctl: FunctionValue<'ctx>,
    pub(crate) s: FunctionValue<'ctx>,
    pub(crate) s_adj: FunctionValue<'ctx>,
    pub(crate) t: FunctionValue<'ctx>,
    pub(crate) t_adj: FunctionValue<'ctx>,
}

impl<'ctx> Intrinsics<'ctx> {
    pub fn new(module: &Module<'ctx>) -> Self {
        Intrinsics {
            m: Intrinsics::get_intrinsic_function_body(module, M),
            r_x: Intrinsics::get_intrinsic_function_body(module, R_X),
            r_y: Intrinsics::get_intrinsic_function_body(module, R_Y),
            r_z: Intrinsics::get_intrinsic_function_body(module, R_Z),
            reset: Intrinsics::get_intrinsic_function_body(module, RESET),
            h: Intrinsics::get_intrinsic_function_body(module, H),
            x: Intrinsics::get_intrinsic_function_body(module, X),
            x_ctl: Intrinsics::get_intrinsic_function_ctl(module, X),
            y: Intrinsics::get_intrinsic_function_body(module, Y),
            y_ctl: Intrinsics::get_intrinsic_function_ctl(module, Y),
            z: Intrinsics::get_intrinsic_function_body(module, Z),
            z_ctl: Intrinsics::get_intrinsic_function_ctl(module, Z),
            s: Intrinsics::get_intrinsic_function_body(module, S),
            s_adj: Intrinsics::get_intrinsic_function_adj(module, S),
            t: Intrinsics::get_intrinsic_function_body(module, T),
            t_adj: Intrinsics::get_intrinsic_function_adj(module, T),
        }
    }

    fn get_intrinsic_function_body(module: &Module<'ctx>, name: &str) -> FunctionValue<'ctx> {
        let function_name = format!("Microsoft__Quantum__Intrinsic__{}__body", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_intrinsic_function_ctl(module: &Module<'ctx>, name: &str) -> FunctionValue<'ctx> {
        let function_name = format!("Microsoft__Quantum__Intrinsic__{}__ctl", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_intrinsic_function_adj(module: &Module<'ctx>, name: &str) -> FunctionValue<'ctx> {
        let function_name = format!("Microsoft__Quantum__Intrinsic__{}__adj", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_function(module: &Module<'ctx>, function_name: &str) -> FunctionValue<'ctx> {
        let function = module.get_function(&function_name).unwrap();

        function
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
