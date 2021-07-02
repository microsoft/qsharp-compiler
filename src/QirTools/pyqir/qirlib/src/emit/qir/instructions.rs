use super::EmitterContext;
use crate::interop::Instruction;
use inkwell::values::{BasicValue, BasicValueEnum, FunctionValue};
use std::collections::BTreeMap;

pub(crate) fn emit<'ctx>(
    context: &EmitterContext<'ctx>,
    inst: &Instruction,
    qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
) -> () {
    emit_call_for_instruction(context, inst, qubits);
}

pub(crate) fn emit_call<'ctx>(
    context: &EmitterContext<'ctx>,
    function: FunctionValue<'ctx>,
    args: &[BasicValueEnum<'ctx>],
) {
    let _ = context
        .module_ctx
        .builder
        .build_call(function, args, "")
        .try_as_basic_value()
        .right()
        .unwrap();
}

pub(crate) fn emit_call_with_return<'ctx>(
    context: &EmitterContext<'ctx>,
    function: FunctionValue<'ctx>,
    args: &[BasicValueEnum<'ctx>],
    name: &String,
) {
    let _ = context
        .module_ctx
        .builder
        .build_call(function, args, name)
        .try_as_basic_value()
        .left()
        .unwrap();
}

fn get_qubit<'ctx>(
    name: &String,
    qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
) -> BasicValueEnum<'ctx> {
    qubits.get(name).unwrap().to_owned()
}

fn get_f64_arg<'ctx>(context: &EmitterContext<'ctx>, value: &f64) -> BasicValueEnum<'ctx> {
    context
        .types
        .double
        .const_float(value.clone())
        .as_basic_value_enum()
}

fn emit_call_for_instruction<'ctx>(
    context: &EmitterContext<'ctx>,
    inst: &Instruction,
    qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
) {
    let intrinsics = &context.intrinsics;
    let find_qubit = |name| get_qubit(name, qubits);

    match inst {
        Instruction::Cx { control, target } => emit_call(
            context,
            intrinsics.X_Ctl,
            &[find_qubit(control), find_qubit(target)],
        ),
        Instruction::Cz { control, target } => emit_call(
            context,
            intrinsics.Z_Ctl,
            &[find_qubit(control), find_qubit(target)],
        ),
        Instruction::H(name) => emit_call(context, intrinsics.H, &[find_qubit(name)]),
        Instruction::M { qubit, target } => {
            emit_call_with_return(context, intrinsics.M, &[find_qubit(qubit)], target)
        }
        Instruction::Reset(name) => emit_call(context, intrinsics.Reset, &[find_qubit(name)]),
        Instruction::Rx { theta, qubit } => emit_call(
            context,
            intrinsics.Rx,
            &[get_f64_arg(context, theta), find_qubit(qubit)],
        ),
        Instruction::Ry { theta, qubit } => emit_call(
            context,
            intrinsics.Ry,
            &[get_f64_arg(context, theta), find_qubit(qubit)],
        ),
        Instruction::Rz { theta, qubit } => emit_call(
            context,
            intrinsics.Rz,
            &[get_f64_arg(context, theta), find_qubit(qubit)],
        ),
        Instruction::S(name) => emit_call(context, intrinsics.S, &[find_qubit(name)]),
        Instruction::Sdg(name /*todo!*/) => {
            emit_call(context, intrinsics.S_Adj, &[find_qubit(name)])
        }
        Instruction::T(name) => emit_call(context, intrinsics.T, &[find_qubit(name)]),
        Instruction::Tdg(name /*todo!*/) => {
            emit_call(context, intrinsics.T_Adj, &[find_qubit(name)])
        }
        Instruction::X(name) => emit_call(context, intrinsics.X, &[find_qubit(name)]),
        Instruction::Y(name) => emit_call(context, intrinsics.Y, &[find_qubit(name)]),
        Instruction::Z(name) => emit_call(context, intrinsics.Z, &[find_qubit(name)]),
    }
}
