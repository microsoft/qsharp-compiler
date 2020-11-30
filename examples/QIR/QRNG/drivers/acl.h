#pragma once

#include <stdint.h>
#include <stdbool.h>
#include "exuberry.h"

// CFG defines for different ADC enables
#define ACL_CFG_ALL 0xFF
#define ACL_CFG_EVEN 0x55
#define ACL_CFG_ODD 0xAA

#define ACL_DIG_ALL 0xFF
#define ACL_ANA_ALL 0xFF

#define NUM_ADCS 8
#define NUM_BITS 12

typedef struct aclMemResult {
    uint16_t cfgEnabled; // bitmask which adcs are enabled
    uint32_t samples;    // how many samples per capture
    int16_t *data[NUM_ADCS];   // array of samples for each adc
    uint8_t num_enabled;
    uint32_t totalSamples;
} aclMemResult;


typedef enum aclConfig {
    aclAllEnabled,
    aclSingleEnable,
    aclEvenEnable,
    aclOddEnable
} aclConfig;

typedef enum aclStatus {
    aclStatusDone    = 0x01,
    aclStatusCapture = 0x02,
    aclStatusDelay   = 0x04,
    aclStatusIdle    = 0x08
} aclStatus;


void acl_perform_capture(bool sleep);

bool acl_init_results(aclConfig cfg, uint32_t samples, aclMemResult *res);

void acl_read_mem(aclMemResult *res);

static __INLINE aclStatus
acl_get_status(void)
{
    return (aclStatus)FKB_DIG_ACL_CSR_RAGS1_FSM_GET(ACL_REGS->rAgs1);
}

static __INLINE void
acl_enable_interrupt(void)
{
    ACL_REGS->rActl =
        FKB_DIG_ACL_CSR_RACTL_ACCIM_MODIFY(ACL_REGS->rActl, 0);
}

static __INLINE void
acl_disable_interrupt(void)
{
    ACL_REGS->rActl =
        FKB_DIG_ACL_CSR_RACTL_ACCIM_MODIFY(ACL_REGS->rActl, 1);
}

static __INLINE bool
acl_get_fifo_full(void)
{
    return FKB_DIG_ACL_CSR_RAGS1_FULL_GET(ACL_REGS->rAgs1);
}

static __INLINE bool
acl_get_fifo_empty(void)
{
    return FKB_DIG_ACL_CSR_RAGS1_EMPTY_GET(ACL_REGS->rAgs1);
}

static __INLINE uint8_t
acl_get_fifo_count(void)
{
    return FKB_DIG_ACL_CSR_RAGS1_WORDCNT_GET(ACL_REGS->rAgs1);
}

static __INLINE void
acl_start_capture(void)
{
    /* Rising edge on this signal kicks off capture */
    ACL_REGS->rAdcctl1 = FKB_DIG_ACL_CSR_RADCCTL1_SEQUENCEEN_SET(1u);
    ACL_REGS->rActl =
        FKB_DIG_ACL_CSR_RACTL_CAPTEN_MODIFY(ACL_REGS->rActl, 1u);
}

static __INLINE void
acl_clear_capture(void)
{
    ACL_REGS->rActl =
        FKB_DIG_ACL_CSR_RACTL_CAPTEN_MODIFY(ACL_REGS->rActl,0u);
    ACL_REGS->rAdcctl1 = FKB_DIG_ACL_CSR_RADCCTL1_SEQUENCEEN_SET(0u);
}

static __INLINE void
acl_set_sampleCnt(uint16_t val)
{
    ACL_REGS->rAcfg0 =
        FKB_DIG_ACL_CSR_RACFG0_SMPLPKTWRITTENCNTINIT_MODIFY(ACL_REGS->rAcfg0,
                                                            val);
}

static __INLINE void
acl_set_capture_adc(uint8_t adc)
{
    /* When in single capture mode, select which adc to read from*/
    ACL_REGS->rAcfg0 =
        FKB_DIG_ACL_CSR_RACFG0_CAPTSINGLESEL_MODIFY(ACL_REGS->rAcfg0, adc);
}

static __INLINE uint8_t
acl_get_capture_adc(void)
{
    /* When in single capture mode, select which adc to read from*/
    return FKB_DIG_ACL_CSR_RACFG0_CAPTSINGLESEL_GET(ACL_REGS->rAcfg0);
}

static __INLINE void
acl_set_config(aclConfig cfg)
{
    ACL_REGS->rAcfg0 =
        FKB_DIG_ACL_CSR_RACFG0_CAPTCFG_MODIFY(ACL_REGS->rAcfg0, cfg);
}


static __INLINE void
acl_ctrl_enable(uint16_t digEn, bool ctlEn, bool ibiasEn)
{
    ACL_REGS->rAdcctl0 = FKB_DIG_ACL_CSR_RADCCTL0_DIGEN_SET(digEn)
        | FKB_DIG_ACL_CSR_RADCCTL0_CTLEN_SET(ctlEn)
        | FKB_DIG_ACL_CSR_RADCCTL0_CMPEN_SET(1)
        | FKB_DIG_ACL_CSR_RADCCTL0_BUFEN_SET(1)
        | FKB_DIG_ACL_CSR_RADCCTL0_GMEN_SET(1)
        | FKB_DIG_ACL_CSR_RADCCTL0_IBIASEN_SET(ibiasEn);
}

static __INLINE uint16_t
acl_read_mem_16(uint32_t offset)
{
    uint16_t *mem = (uint16_t *)ACL_BASE;
    return *(mem + offset);
}

static __INLINE bool
acl_get_isr_done(void)
{
    return FKB_DIG_ACL_CSR_RACTL_ACCIF_GET(ACL_REGS->rActl);
}

#ifdef ACL_INCLUDE_ISR
extern volatile bool aclCaptureDone;
void
ACL_Handler(void)
{
    aclCaptureDone = true;

    /* Clear the capture Flag
       This needs to be done within the ISR to clear the ISR pending flag.
       The ISR flag itself then needs to be manually cleared.
    */
    acl_clear_capture();

    // clear the pending interrupt flag
    ACL_REGS->rActl =
        FKB_DIG_ACL_CSR_RACTL_ACCIF_MODIFY(ACL_REGS->rActl, 0);
    // kick the sleeping core
    __SEV();
}
#endif
