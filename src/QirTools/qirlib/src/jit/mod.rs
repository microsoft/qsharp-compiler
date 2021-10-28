use inkwell::OptimizationLevel;

pub struct Context<'ctx> {
    pub(crate) context: &'ctx inkwell::context::Context,
    pub(crate) module: inkwell::module::Module<'ctx>,
    pub(crate) builder: inkwell::builder::Builder<'ctx>,
    pub(crate) execution_engine: inkwell::execution_engine::ExecutionEngine<'ctx>,
}

impl<'ctx> Context<'ctx> {
    pub fn new(context: &'ctx inkwell::context::Context, name: &'ctx str) -> Self {
        let builder = context.create_builder();
        let module = context.create_module(name);

        let execution_engine = module
            .create_jit_execution_engine(OptimizationLevel::None)
            .expect("Could not create JIT Engine");
        Context {
            builder,
            module,
            context,
            execution_engine,
        }
    }
}

#[cfg(test)]
mod tests {
    use crate::emit::populate_context;
    use crate::interop::pyjit::runtime::Simulator;
    use crate::interop::{ClassicalRegister, QuantumRegister, SemanticModel};
    use crate::interop::{Controlled, Instruction, Single};
    use crate::jit::Context;
    use inkwell::execution_engine::JitFunction;
    use inkwell::passes::PassManager;
    use inkwell::targets::TargetMachine;
    use inkwell::{
        passes::PassManagerBuilder,
        targets::{InitializationConfig, Target},
        OptimizationLevel,
    };
    use microsoft_quantum_qir_runtime_sys::BasicRuntimeDriver;
    use tempfile::tempdir;
    type SumU64 = unsafe extern "C" fn(u64, u64) -> u64;

    #[test]
    fn jit_compilation_of_simple_function() {
        let ctx = inkwell::context::Context::create();
        let name = "jit_compilation_of_simple_function";
        let context = Context::new(&ctx, name);

        let sum = jit_compile_sumu64(context, name).expect("Unable to JIT compile sum function");

        let x = 1u64;
        let y = 2u64;

        unsafe {
            assert_eq!(sum.call(x, y), x + y);
        }
    }

    fn jit_compile_sumu64<'ctx>(
        context: Context<'ctx>,
        name: &'ctx str,
    ) -> Option<JitFunction<'ctx, SumU64>> {
        let i64_type = context.context.i64_type();
        let fn_type = i64_type.fn_type(&[i64_type.into(), i64_type.into()], false);
        let function = context.module.add_function(name, fn_type, None);
        let basic_block = context.context.append_basic_block(function, "entry");

        context.builder.position_at_end(basic_block);

        let x = function.get_nth_param(0)?.into_int_value();
        let y = function.get_nth_param(1)?.into_int_value();

        let sum = context.builder.build_int_add(x, y, name);

        context.builder.build_return(Some(&sum));

        unsafe { context.execution_engine.get_function(name).ok() }
    }

    #[test]
    fn eval_test() {
        let dir = tempdir().expect("");
        let tmp_path = dir.into_path();
        let name = "bell_measure";
        let file_path = tmp_path.join(format!("{}.ll", name));

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

        // model.add_inst(Instruction::M(Measured::new(
        //     String::from("qr0"),
        //     String::from("qc0"),
        // )));
        // model.add_inst(Instruction::M(Measured::new(
        //     String::from("qr1"),
        //     String::from("qc1"),
        // )));

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

        log::info!("Writing {:?}", file_path);
        println!("Writing {:?}", file_path);
        context
            .emit_ir(file_path.display().to_string().as_str())
            .unwrap();

        unsafe {
            BasicRuntimeDriver::initialize_qir_context(true);
            let _foundation = microsoft_quantum_qir_qsharp_foundation_sys::QSharpFoundation::new();

            let ee = context
                .module
                .create_jit_execution_engine(OptimizationLevel::None)
                .unwrap();
            let simulator = Simulator::new(&context, &ee);

            let main = ee
                .get_function::<unsafe extern "C" fn() -> ()>("QuantumApplication__Run")
                .unwrap();

            main.call();
            let generated_model = simulator.get_model();
            assert!(generated_model.instructions.len() == 2)
        }
    }

    // #[test]
    // #[ignore = "Needs Runtime package to run"]
    // fn generate_output_function() {
    //     let dir = tempdir().expect("");
    //     let tmp_path = dir.into_path();
    //     let name = "bell_measure";
    //     let file_path = tmp_path.join(format!("{}.ll", name));
    //     let asm_path = tmp_path.join(format!("{}.asm", name));
    //     let obj_path = tmp_path.join(format!("{}.o", name));

    //     let name = String::from("Bell circuit");
    //     let mut model = SemanticModel::new(name);
    //     model.add_reg(QuantumRegister::new(String::from("qr"), 0).as_register());
    //     model.add_reg(QuantumRegister::new(String::from("qr"), 1).as_register());
    //     model.add_reg(ClassicalRegister::new(String::from("qc"), 2).as_register());

    //     model.add_inst(Instruction::H(Single::new(String::from("qr0"))));
    //     model.add_inst(Instruction::Cx(Controlled::new(
    //         String::from("qr0"),
    //         String::from("qr1"),
    //     )));
    //     model.add_inst(Instruction::DumpMachine);
    //     model.add_inst(Instruction::M(Measured::new(
    //         String::from("qr0"),
    //         String::from("qc0"),
    //     )));
    //     model.add_inst(Instruction::M(Measured::new(
    //         String::from("qr1"),
    //         String::from("qc1"),
    //     )));

    //     let ctx = inkwell::context::Context::create();
    //     let context = populate_context(&ctx, &model).unwrap();

    //     Target::initialize_x86(&InitializationConfig::default());

    //     let opt = OptimizationLevel::Default;
    //     let reloc = RelocMode::Default;
    //     let model = CodeModel::Default;
    //     let default_triple = TargetMachine::get_default_triple();

    //     let target = Target::from_triple(&default_triple).expect("Unable to create target machine");
    //     let target_machine = target
    //         .create_target_machine(&default_triple, "x86-64", "", opt, reloc, model)
    //         .expect("Unable to create target machine");

    //     assert!(target.has_asm_backend());
    //     assert!(target.has_target_machine());

    //     let pass_manager_builder = PassManagerBuilder::create();
    //     pass_manager_builder.set_optimization_level(OptimizationLevel::None);

    //     let fpm = PassManager::create(());
    //     fpm.add_global_dce_pass();
    //     fpm.add_strip_dead_prototypes_pass();
    //     pass_manager_builder.populate_module_pass_manager(&fpm);
    //     fpm.run_on(&context.module);

    //     log::info!("Writing {:?}", file_path);
    //     println!("Writing {:?}", file_path);
    //     context
    //         .emit_ir(file_path.display().to_string().as_str())
    //         .unwrap();

    //     log::info!("Writing {:?}", asm_path);
    //     println!("Writing {:?}", asm_path);
    //     assert!(target_machine
    //         .write_to_file(&context.module, FileType::Assembly, &asm_path)
    //         .is_ok());
    //     log::info!("Writing {:?}", obj_path);
    //     println!("Writing {:?}", obj_path);
    //     assert!(target_machine
    //         .write_to_file(&context.module, FileType::Object, &obj_path)
    //         .is_ok());
    //     unsafe {
    //         let runtime_path = std::env::var("QSHARP_RUNTIME_PATH").unwrap();
    //         let runtime = Runtime::new(&PathBuf::from(runtime_path)).unwrap();
    //         let simulator_path = std::env::var("QSHARP_NATIVE_SIM_PATH").unwrap();
    //         let _simulator = Simulator::new(&PathBuf::from(simulator_path)).unwrap();

    //         let driver = runtime.CreateFullstateSimulatorC(rand::prelude::random::<i32>());
    //         runtime.InitializeQirContext(driver, true);

    //         let ee = context
    //             .module
    //             .create_jit_execution_engine(OptimizationLevel::None)
    //             .unwrap();

    //         let main = ee
    //             .get_function::<unsafe extern "C" fn() -> i64>("QuantumApplication__Run")
    //             .unwrap();
    //         for _ in 1..10 {
    //             main.call();
    //         }
    //     }
    // }
}
