#pragma once


#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include "exuberry.h"

typedef enum rclMode {
    rclModeSingle = 0,
    rclModeContinuous
} rclMode;

typedef enum rclFsmStatus {
    rclStatus_draining   = 0x01,
    rclStatus_sendZero   = 0x02,
    rclStatus_sendReplay = 0x04,
    rclStatus_sendBuffer = 0x08,
    rclStatus_idle       = 0x10
} rclFsmStatus;

static __INLINE void
rcl_start(void)
{
    RCL_REGS->rRctl = FKB_DIG_RCL_CSR_RRCTL_FSMACTIVE_SET(1);
}

static __INLINE void
rcl_stop(void)
{
    RCL_REGS->rRctl = FKB_DIG_RCL_CSR_RRCTL_FSMACTIVE_SET(0);
}

static __INLINE void
rcl_set_mode(rclMode mode)
{
    RCL_REGS->rRcfg0 =
     FKB_DIG_RCL_CSR_RRCFG0_MODE_MODIFY(RCL_REGS->rRcfg0, mode);
}

static __INLINE void
rcl_set_sampleCnt(uint16_t count)
{
    ASSERT(count != 0);
    RCL_REGS->rRcfg0 =
     FKB_DIG_RCL_CSR_RRCFG0_SAMPLECNTPLUSONEINIT_MODIFY(RCL_REGS->rRcfg0,
                                                        count-1);
}

static __INLINE void
rcl_set_replayCnt(uint32_t count)
{
    RCL_REGS->rRcfg1 =
      FKB_DIG_RCL_CSR_RRCFG1_REPLAYCNTINIT_SET(count);
}

static __INLINE rclFsmStatus
rcl_get_fsm_status(void)
{
    return (rclFsmStatus)FKB_DIG_RCL_CSR_RRGS0_FSM_GET(RCL_REGS->rRgs0);
}

static __INLINE uint16_t
rcl_get_loopCnt(void)
{
    return FKB_DIG_RCL_CSR_RRGS0_LOOPCNT_GET(RCL_REGS->rRgs0);
}

/* Write to the memory */
static __INLINE void
rcl_write_memory(uint32_t *mem, size_t size)
{
    uint32_t *rcl_mem;
    int i;
    rcl_mem = (uint32_t *)RCL_REGS;

    (void)i;
#if 1
    memcpy(rcl_mem, mem, size);
#else
    for (i = 0; i < size/4; i++) {
        rcl_mem[i] = mem[i];
    }
#endif

}
