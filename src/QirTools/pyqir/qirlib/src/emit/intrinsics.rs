use inkwell::context::Context;
use inkwell::module::Module;
use inkwell::types::FloatType;
use inkwell::types::IntType;
use inkwell::types::PointerType;
use inkwell::types::StructType;
use inkwell::AddressSpace;
use inkwell::values::FunctionValue;

use super::EmitterContext;

pub const M: &str = "M";
pub const Rx: &str = "Rx";
pub const Ry: &str = "Ry";
pub const Rz: &str = "Rz";
pub const Reset: &str = "Reset";

pub const X: &str = "x";
pub const Y: &str = "y";
pub const Z: &str = "z";

pub const S: &str = "s";
pub const T: &str = "t";

// TODO: pull the function names or the called instructions?
//define internal void @Microsoft__Quantum__Intrinsic__S__body(%Qubit* %qubit) {

struct Intrinsics<'ctx> {
    M: FunctionValue<'ctx>,
    // void @Microsoft__Quantum__Intrinsic__Rx__body(double %theta, %Qubit* %qubit)
    Rx: FunctionValue<'ctx>,
    Ry: FunctionValue<'ctx>,
    Rz: FunctionValue<'ctx>,
    Reset: FunctionValue<'ctx>,
    // void @__quantum__qis__h__body(%Qubit* %qubit)
    //H: FunctionValue<'ctx>,
    // void @__quantum__qis__x__body(%Qubit*)
    X: FunctionValue<'ctx>,
    // void @__quantum__qis__x__ctl(%Array*, %Qubit*)
    X_Ctl: FunctionValue<'ctx>,
    // void @__quantum__qis__y__body(%Qubit*)
    Y: FunctionValue<'ctx>,
    // void @__quantum__qis__y__ctl(%Array*, %Qubit*)
    Y_Ctl: FunctionValue<'ctx>,
    // void @__quantum__qis__z__body(%Qubit*)
    Z: FunctionValue<'ctx>,
    // void @__quantum__qis__z__ctl(%Array*, %Qubit*)
    Z_Ctl: FunctionValue<'ctx>,
    // void @__quantum__qis__s__body(%Qubit* %qubit)
    S: FunctionValue<'ctx>,
    // void @__quantum__qis__s__adj(%Qubit* %qubit)
    S_Adj: FunctionValue<'ctx>,
    // void @__quantum__qis__t__body(%Qubit* %qubit)
    T: FunctionValue<'ctx>,
    // void @__quantum__qis__t__adj(%Qubit* %qubit)
    T_Adj: FunctionValue<'ctx>,
}

impl<'ctx> Intrinsics<'ctx> {
    pub fn new(module: &Module<'ctx>) -> Self {
        Intrinsics {
            M : Intrinsics::get_intrinsic_function(module, M),
            Rx : Intrinsics::get_intrinsic_function(module, Rx),
            Ry : Intrinsics::get_intrinsic_function(module, Ry),
            Rz : Intrinsics::get_intrinsic_function(module, Rz),
            Reset : Intrinsics::get_intrinsic_function(module, Reset),
            //H : Intrinsics::get_qis_function(module, H),
            X : Intrinsics::get_qis_function(module, X),
            X_Ctl : Intrinsics::get_qis_ctl_function(module, X),
            Y : Intrinsics::get_qis_function(module, Y),
            Y_Ctl : Intrinsics::get_qis_ctl_function(module, Y),
            Z : Intrinsics::get_qis_function(module, Z),
            Z_Ctl : Intrinsics::get_qis_ctl_function(module, Z),
            S : Intrinsics::get_qis_function(module, S),
            S_Adj : Intrinsics::get_qis_adj_function(module, S),
            T : Intrinsics::get_qis_function(module, T),
            T_Adj : Intrinsics::get_qis_adj_function(module, T),
        }
    }

    fn get_intrinsic_function(module: &Module<'ctx>, name : &str) -> FunctionValue<'ctx> {
        let function_name = format!("Microsoft__Quantum__Intrinsic__{}__body", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_qis_function(module: &Module<'ctx>, name : &str) -> FunctionValue<'ctx> {
        let function_name = format!("__quantum__qis__{}__body", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_qis_adj_function(module: &Module<'ctx>, name : &str) -> FunctionValue<'ctx> {
        let function_name = format!("__quantum__qis__{}__adj", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_qis_ctl_function(module: &Module<'ctx>, name : &str) -> FunctionValue<'ctx> {
        let function_name = format!("__quantum__qis__{}__ctl", name);
        Intrinsics::get_function(module, function_name.as_str())
    }

    fn get_function(module: &Module<'ctx>, function_name : &str) -> FunctionValue<'ctx> {
        let function = 
            module
            .get_function(&function_name)
            .unwrap();

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
