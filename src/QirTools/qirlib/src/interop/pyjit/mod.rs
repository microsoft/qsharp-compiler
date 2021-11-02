// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use crate::emit::Context;
use crate::interop::pyjit::runtime::Simulator;
use crate::interop::SemanticModel;
use inkwell::passes::PassManager;
use inkwell::targets::TargetMachine;
use inkwell::{
    passes::PassManagerBuilder,
    targets::{InitializationConfig, Target},
    OptimizationLevel,
};
use microsoft_quantum_qir_runtime_sys::BasicRuntimeDriver;

pub mod gates;
mod intrinsics;
pub mod runtime;

pub fn run_ctx<'ctx>(context: Context<'ctx>) -> Result<SemanticModel, String> {
    Target::initialize_native(&InitializationConfig::default()).unwrap();

    let default_triple = TargetMachine::get_default_triple();

    let target = Target::from_triple(&default_triple).expect("Unable to create target machine");

    assert!(target.has_asm_backend());
    assert!(target.has_target_machine());

    run_basic_passes_on(&context);

    unsafe {
        BasicRuntimeDriver::initialize_qir_context(true);
        let _ = microsoft_quantum_qir_qsharp_foundation_sys::QSharpFoundation::new();

        let _ = inkwell::support::load_library_permanently("");
        let simulator = Simulator::new(&context, &context.execution_engine);
        let main = context
            .execution_engine
            .get_function::<unsafe extern "C" fn() -> ()>("QuantumApplication__Run")
            .unwrap();
        main.call();
        Ok(simulator.get_model())
    }
}

pub fn run_basic_passes_on<'ctx>(context: &Context<'ctx>) -> bool {
    let pass_manager_builder = PassManagerBuilder::create();
    pass_manager_builder.set_optimization_level(OptimizationLevel::None);
    let fpm = PassManager::create(());
    fpm.add_global_dce_pass();
    fpm.add_strip_dead_prototypes_pass();
    pass_manager_builder.populate_module_pass_manager(&fpm);
    fpm.run_on(&context.module)
}

#[cfg(test)]
mod tests {
    use crate::emit::populate_context;
    use crate::interop::pyjit::run_ctx;
    use crate::interop::{ClassicalRegister, Measured, QuantumRegister, SemanticModel};
    use crate::interop::{Controlled, Instruction, Single};
    use tempfile::tempdir;

    #[ignore = "CI Requires runtime recompilation"]
    #[test]
    fn eval_test() -> Result<(), String> {
        let dir = tempdir().expect("");
        let tmp_path = dir.into_path();

        let name = String::from("Bell circuit");
        let mut model = SemanticModel::new(name);
        model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
        model.add_reg(QuantumRegister::new(String::from("qr"), 1).as_register());
        model.add_reg(ClassicalRegister::new(String::from("qc"), 2).as_register());

        model.add_inst(Instruction::H(Single::new(String::from("qr0"))));
        model.add_inst(Instruction::Cx(Controlled::new(
            String::from("qr0"),
            String::from("qr1"),
        )));

        model.add_inst(Instruction::M(Measured::new(
            String::from("qr0"),
            String::from("qc0"),
        )));
        model.add_inst(Instruction::M(Measured::new(
            String::from("qr1"),
            String::from("qc1"),
        )));

        let ctx = inkwell::context::Context::create();
        let context = populate_context(&ctx, &model).unwrap();

        let file_path = tmp_path.join(format!(
            "{}.ll",
            context.module.get_name().to_str().unwrap()
        ));
        println!("Writing {:?}", file_path);
        context
            .emit_ir(file_path.display().to_string().as_str())
            .unwrap();

        let generated_model = run_ctx(context)?;

        assert!(generated_model.instructions.len() == 2);
        Ok(())
    }
}
