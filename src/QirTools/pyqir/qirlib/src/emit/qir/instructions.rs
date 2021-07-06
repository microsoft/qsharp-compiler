use super::{array1d::create_ctl_wrapper, basic_values, calls};
use crate::{emit::Context, interop::Instruction};
use inkwell::values::{BasicValueEnum};
use std::collections::BTreeMap;


fn get_qubit<'ctx>(
    name: &String,
    qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
) -> BasicValueEnum<'ctx> {
    qubits.get(name).unwrap().to_owned()
}

pub(crate) fn emit<'ctx>(
    context: &Context<'ctx>,
    inst: &Instruction,
    qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
) -> () {
    let intrinsics = &context.intrinsics;
    let find_qubit = |name| get_qubit(name, qubits);
    let ctl = |value| create_ctl_wrapper(context, value);
    match inst {
        Instruction::Cx(inst) => calls::emit_void_call(
            context,
            intrinsics.x_ctl,
            &[ctl(&find_qubit(&inst.control)), find_qubit(&inst.target)],
        ),
        Instruction::Cz(inst) => calls::emit_void_call(
            context,
            intrinsics.z_ctl,
            &[ctl(&find_qubit(&inst.control)), find_qubit(&inst.target)],
        ),
        Instruction::H(inst) => calls::emit_void_call(context, intrinsics.h, &[find_qubit(&inst.qubit)]),
        Instruction::M { qubit, target } => {
            calls::emit_call_with_return(context, intrinsics.m, &[find_qubit(qubit)], target);
        }
        Instruction::Reset(inst) => calls::emit_void_call(context, intrinsics.reset, &[find_qubit(&inst.qubit)]),
        Instruction::Rx (inst) => calls::emit_void_call(
            context,
            intrinsics.r_x,
            &[basic_values::f64_to_f64(context, &inst.theta), find_qubit(&inst.qubit)],
        ),
        Instruction::Ry (inst) => calls::emit_void_call(
            context,
            intrinsics.r_y,
            &[basic_values::f64_to_f64(context, &inst.theta), find_qubit(&inst.qubit)],
        ),
        Instruction::Rz (inst) => calls::emit_void_call(
            context,
            intrinsics.r_z,
            &[basic_values::f64_to_f64(context, &inst.theta), find_qubit(&inst.qubit)],
        ),
        Instruction::S(inst) => calls::emit_void_call(context, intrinsics.s, &[find_qubit(&inst.qubit)]),
        Instruction::Sdg(inst) => calls::emit_void_call(context, intrinsics.s_adj, &[find_qubit(&inst.qubit)]),
        Instruction::T(inst) => calls::emit_void_call(context, intrinsics.t, &[find_qubit(&inst.qubit)]),
        Instruction::Tdg(inst) => calls::emit_void_call(context, intrinsics.t_adj, &[find_qubit(&inst.qubit)]),
        Instruction::X(inst) => calls::emit_void_call(context, intrinsics.x, &[find_qubit(&inst.qubit)]),
        Instruction::Y(inst) => calls::emit_void_call(context, intrinsics.y, &[find_qubit(&inst.qubit)]),
        Instruction::Z(inst) => calls::emit_void_call(context, intrinsics.z, &[find_qubit(&inst.qubit)]),
    }
}
