use inkwell::{
    passes::{PassManager, PassManagerBuilder},
    targets::{InitializationConfig, Target, TargetMachine},
    OptimizationLevel,
};
use microsoft_quantum_qir_runtime_sys::BasicRuntimeDriver;

use crate::{emit::populate_context, interop::pyjit::runtime::Simulator};

use super::SemanticModel;

pub fn write(model: &SemanticModel, file_name: &str) -> Result<(), String> {
    let ctx = inkwell::context::Context::create();
    let context = populate_context(&ctx, &model)?;

    context.emit_ir(file_name)?;

    Ok(())
}

pub fn get_ir_string(model: &SemanticModel) -> Result<String, String> {
    let ctx = inkwell::context::Context::create();
    let context = populate_context(&ctx, &model)?;

    let ir = context.get_ir_string();

    Ok(ir)
}

pub fn get_bitcode_base64_string(model: &SemanticModel) -> Result<String, String> {
    let ctx = inkwell::context::Context::create();
    let context = populate_context(&ctx, &model)?;

    let b64 = context.get_bitcode_base64_string();

    Ok(b64)
}

pub fn run(model: &SemanticModel) -> Result<SemanticModel, String> {
    let ctx = inkwell::context::Context::create();
    let context = populate_context(&ctx, &model).unwrap();

    Target::initialize_native(&InitializationConfig::default()).unwrap();

    let default_triple = TargetMachine::get_default_triple();

    let target = Target::from_triple(&default_triple).expect("Unable to create target machine");

    assert!(target.has_asm_backend());
    assert!(target.has_target_machine());

    let pass_manager_builder = PassManagerBuilder::create();
    pass_manager_builder.set_optimization_level(OptimizationLevel::None);
    let fpm = PassManager::create(());
    fpm.add_global_dce_pass();
    fpm.add_strip_dead_prototypes_pass();
    pass_manager_builder.populate_module_pass_manager(&fpm);
    fpm.run_on(&context.module);

    unsafe {
        BasicRuntimeDriver::initialize_qir_context(true);
        let _ = microsoft_quantum_qir_qsharp_foundation_sys::QSharpFoundation::new().unwrap();

        let ee = context
            .module
            .create_jit_execution_engine(OptimizationLevel::None)
            .unwrap();

        let _ = inkwell::support::load_library_permanently("");
        let simulator = Simulator::new(&context, &ee);
        let main = ee
            .get_function::<unsafe extern "C" fn() -> ()>("QuantumApplication__Run")
            .unwrap();
        main.call();
        Ok(simulator.get_model())
    }
}
