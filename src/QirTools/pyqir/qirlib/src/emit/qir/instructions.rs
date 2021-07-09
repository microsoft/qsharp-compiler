use super::{
    array1d::{self, create_ctl_wrapper},
    basic_values, calls,
};
use crate::{emit::Context, interop::Instruction};
use inkwell::values::{BasicValueEnum, FunctionValue};
use std::collections::BTreeMap;

fn get_qubit<'ctx>(
    name: &String,
    qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
) -> BasicValueEnum<'ctx> {
    qubits.get(name).unwrap().to_owned()
}

fn get_register<'ctx>(
    name: &String,
    registers: &BTreeMap<String, (BasicValueEnum<'ctx>, Option<u64>)>,
) -> (BasicValueEnum<'ctx>, Option<u64>) {
    registers.get(name).unwrap().to_owned()
}

pub(crate) fn emit<'ctx>(
    context: &Context<'ctx>,
    inst: &Instruction,
    qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
    registers: &BTreeMap<String, (BasicValueEnum<'ctx>, Option<u64>)>,
) -> () {
    let intrinsics = &context.intrinsics;
    let find_qubit = |name| get_qubit(name, qubits);
    let ctl = |value| create_ctl_wrapper(context, value);
    match inst {
        Instruction::Cx(inst) => {
            let control = ctl(&find_qubit(&inst.control));
            let qubit = find_qubit(&inst.target);
            controlled(context, intrinsics.x_ctl, control, qubit);
        }
        Instruction::Cz(inst) => {
            let control = ctl(&find_qubit(&inst.control));
            let qubit = find_qubit(&inst.target);
            controlled(context, intrinsics.z_ctl, control, qubit);
        }
        Instruction::H(inst) => {
            calls::emit_void_call(context, intrinsics.h, &[find_qubit(&inst.qubit)])
        }
        Instruction::M { qubit, target } => {
            measure(context, qubit, target, qubits, registers);
        }
        Instruction::Reset(inst) => {
            calls::emit_void_call(context, intrinsics.reset, &[find_qubit(&inst.qubit)])
        }
        Instruction::Rx(inst) => calls::emit_void_call(
            context,
            intrinsics.r_x,
            &[
                basic_values::f64_to_f64(context, &inst.theta),
                find_qubit(&inst.qubit),
            ],
        ),
        Instruction::Ry(inst) => calls::emit_void_call(
            context,
            intrinsics.r_y,
            &[
                basic_values::f64_to_f64(context, &inst.theta),
                find_qubit(&inst.qubit),
            ],
        ),
        Instruction::Rz(inst) => calls::emit_void_call(
            context,
            intrinsics.r_z,
            &[
                basic_values::f64_to_f64(context, &inst.theta),
                find_qubit(&inst.qubit),
            ],
        ),
        Instruction::S(inst) => {
            calls::emit_void_call(context, intrinsics.s, &[find_qubit(&inst.qubit)])
        }
        Instruction::Sdg(inst) => {
            calls::emit_void_call(context, intrinsics.s_adj, &[find_qubit(&inst.qubit)])
        }
        Instruction::T(inst) => {
            calls::emit_void_call(context, intrinsics.t, &[find_qubit(&inst.qubit)])
        }
        Instruction::Tdg(inst) => {
            calls::emit_void_call(context, intrinsics.t_adj, &[find_qubit(&inst.qubit)])
        }
        Instruction::X(inst) => {
            calls::emit_void_call(context, intrinsics.x, &[find_qubit(&inst.qubit)])
        }
        Instruction::Y(inst) => {
            calls::emit_void_call(context, intrinsics.y, &[find_qubit(&inst.qubit)])
        }
        Instruction::Z(inst) => {
            calls::emit_void_call(context, intrinsics.z, &[find_qubit(&inst.qubit)])
        }
    }

    fn measure<'ctx>(
        context: &Context<'ctx>,
        qubit: &String,
        target: &String,
        qubits: &BTreeMap<String, BasicValueEnum<'ctx>>,
        registers: &BTreeMap<String, (BasicValueEnum<'ctx>, Option<u64>)>,
    ) {
        let find_qubit = |name| get_qubit(name, qubits);
        let find_register = |name| get_register(name, registers);

        // measure the qubit and save the result to a temporary value
        let result = calls::emit_call_with_return(
            context,
            context.intrinsics.m,
            &[find_qubit(qubit)],
            "measurement",
        );

        // find the parent register and offset for the given target
        let (register, index) = find_register(target);

        // get the bitcast pointer to the target location
        let bitcast_indexed_target_register = array1d::get_bitcast_result_pointer_array_element(
            context,
            index.unwrap(),
            &register,
            target,
        );

        // get the existing value from that location and decrement its ref count as its
        // being replaced with the measurement.
        let existing_value = context.builder.build_load(
            bitcast_indexed_target_register.into_pointer_value(),
            "existing_value",
        );
        let minus_one = basic_values::i64_to_i32(context, -1);
        context.builder.build_call(
            context.runtime_library.result_update_reference_count,
            &[existing_value, minus_one],
            "",
        );

        // increase the ref count of the new value and store it in the target register
        let one = basic_values::i64_to_i32(context, 1);
        context.builder.build_call(
            context.runtime_library.result_update_reference_count,
            &[result, one],
            "",
        );
        let _ = context
            .builder
            .build_store(bitcast_indexed_target_register.into_pointer_value(), result);
    }

    fn controlled<'ctx>(
        context: &Context<'ctx>,
        intrinsic: FunctionValue<'ctx>,
        control: BasicValueEnum<'ctx>,
        qubit: BasicValueEnum<'ctx>,
    ) {
        calls::emit_void_call(context, intrinsic, &[control, qubit]);
        let minus_one = basic_values::i64_to_i32(context, -1);
        context.builder.build_call(
            context.runtime_library.array_update_reference_count,
            &[control, minus_one],
            "",
        );
    }
}
