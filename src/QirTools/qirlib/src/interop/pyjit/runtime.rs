// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

use crate::emit::intrinsics::Intrinsics;
use crate::emit::Context;
use crate::interop::pyjit::gates::CURRENT_GATES;
use crate::interop::SemanticModel;
use inkwell::execution_engine::ExecutionEngine;

use super::gates::GateScope;

pub(crate) struct Simulator {
    scope: GateScope,
}

impl<'ctx> Simulator {
    pub fn new(context: &Context<'ctx>, ee: &ExecutionEngine<'ctx>) -> Self {
        let simulator = Simulator {
            scope: crate::interop::pyjit::gates::GateScope::new(),
        };
        simulator.bind(context, ee);
        simulator
    }

    pub fn get_model(&self) -> SemanticModel {
        let mut gs = CURRENT_GATES.write().unwrap();
        gs.infer_allocations();
        gs.get_model()
    }

    fn bind(&self, context: &Context<'ctx>, ee: &ExecutionEngine<'ctx>) {
        let intrinsics = Intrinsics::new(&context.module);

        if let Some(ins) = intrinsics.h_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__h__body as usize);
        }

        if let Some(ins) = intrinsics.h_ctl_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__h__ctl as usize);
        }
        if let Some(ins) = intrinsics.m_ins {
            ee.add_global_mapping(
                &ins,
                super::intrinsics::__quantum__qis__measure__body as usize,
            );
        }
        if let Some(ins) = intrinsics.r_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__r__body as usize);
        }
        if let Some(ins) = intrinsics.r_adj_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__r__adj as usize);
        }
        if let Some(ins) = intrinsics.r_ctl_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__r__ctl as usize);
        }
        if let Some(ins) = intrinsics.r_ctl_adj_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__r__ctladj as usize);
        }

        if let Some(ins) = intrinsics.s_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__s__body as usize);
        }
        if let Some(ins) = intrinsics.s_adj_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__s__adj as usize);
        }
        if let Some(ins) = intrinsics.s_ctl_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__s__ctl as usize);
        }
        if let Some(ins) = intrinsics.s_ctl_adj_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__s__ctladj as usize);
        }

        if let Some(ins) = intrinsics.t_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__t__body as usize);
        }
        if let Some(ins) = intrinsics.t_adj_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__t__adj as usize);
        }
        if let Some(ins) = intrinsics.t_ctl_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__t__ctl as usize);
        }
        if let Some(ins) = intrinsics.t_ctl_adj_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__t__ctladj as usize);
        }

        if let Some(ins) = intrinsics.x_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__x__body as usize);
        }
        if let Some(ins) = intrinsics.x_ctl_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__x__ctl as usize);
        }

        if let Some(ins) = intrinsics.y_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__y__body as usize);
        }
        if let Some(ins) = intrinsics.y_ctl_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__y__ctl as usize);
        }

        if let Some(ins) = intrinsics.z_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__z__body as usize);
        }
        if let Some(ins) = intrinsics.z_ctl_ins {
            ee.add_global_mapping(&ins, super::intrinsics::__quantum__qis__z__ctl as usize);
        }

        if let Some(ins) = intrinsics.dumpmachine {
            ee.add_global_mapping(
                &ins,
                super::intrinsics::__quantum__qis__dumpmachine__body as usize,
            );
        }

        if let Some(ins) = intrinsics.dumpregister {
            ee.add_global_mapping(
                &ins,
                super::intrinsics::__quantum__qis__dumpregister__body as usize,
            );
        }
    }
}
