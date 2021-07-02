use inkwell::context::Context;
use inkwell::module::Module;
use inkwell::types::FloatType;
use inkwell::types::IntType;
use inkwell::types::PointerType;
use inkwell::types::StructType;
use inkwell::values::FunctionValue;
use inkwell::AddressSpace;

use super::EmitterContext;

pub const M: &str = "M";
pub const Rx: &str = "Rx";
pub const Ry: &str = "Ry";
pub const Rz: &str = "Rz";
pub const Reset: &str = "Reset";

pub const H: &str = "H";

pub const X: &str = "X";
pub const Y: &str = "Y";
pub const Z: &str = "Z";

pub const S: &str = "S";
pub const T: &str = "T";

pub(crate) struct Intrinsics<'ctx> {
    pub(crate) M: FunctionValue<'ctx>,
    pub(crate) Rx: FunctionValue<'ctx>,
    pub(crate) Ry: FunctionValue<'ctx>,
    pub(crate) Rz: FunctionValue<'ctx>,
    pub(crate) Reset: FunctionValue<'ctx>,
    pub(crate) H: FunctionValue<'ctx>,
    pub(crate) X: FunctionValue<'ctx>,
    pub(crate) X_Ctl: FunctionValue<'ctx>,
    pub(crate) Y: FunctionValue<'ctx>,
    pub(crate) Y_Ctl: FunctionValue<'ctx>,
    pub(crate) Z: FunctionValue<'ctx>,
    pub(crate) Z_Ctl: FunctionValue<'ctx>,
    pub(crate) S: FunctionValue<'ctx>,
    pub(crate) S_Adj: FunctionValue<'ctx>,
    pub(crate) T: FunctionValue<'ctx>,
    pub(crate) T_Adj: FunctionValue<'ctx>,
}

impl<'ctx> Intrinsics<'ctx> {
    pub fn new(module: &Module<'ctx>) -> Self {
        Intrinsics {
            M: Intrinsics::get_intrinsic_function_body(module, M),
            Rx: Intrinsics::get_intrinsic_function_body(module, Rx),
            Ry: Intrinsics::get_intrinsic_function_body(module, Ry),
            Rz: Intrinsics::get_intrinsic_function_body(module, Rz),
            Reset: Intrinsics::get_intrinsic_function_body(module, Reset),
            H: Intrinsics::get_intrinsic_function_body(module, H),
            X: Intrinsics::get_intrinsic_function_body(module, X),
            X_Ctl: Intrinsics::get_intrinsic_function_ctl(module, X),
            Y: Intrinsics::get_intrinsic_function_body(module, Y),
            Y_Ctl: Intrinsics::get_intrinsic_function_ctl(module, Y),
            Z: Intrinsics::get_intrinsic_function_body(module, Z),
            Z_Ctl: Intrinsics::get_intrinsic_function_ctl(module, Z),
            S: Intrinsics::get_intrinsic_function_body(module, S),
            S_Adj: Intrinsics::get_intrinsic_function_adj(module, S),
            T: Intrinsics::get_intrinsic_function_body(module, T),
            T_Adj: Intrinsics::get_intrinsic_function_adj(module, T),
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
    use inkwell::context::Context;

    use crate::emit::ModuleContext;

    use super::*;

    #[test]
    fn intrinsics_can_be_loaded() {
        let ctx = Context::create();
        let name = "temp";
        let module_ctx = ModuleContext::new(&ctx, name);
        let _ = Intrinsics::new(&module_ctx.module);
    }
}
