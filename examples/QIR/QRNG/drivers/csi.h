#pragma once

#include <stdint.h>
#include <stdbool.h>
#include "exuberry.h"


#define CSI_CMD_WRITE 0
#define CSI_CMD_READ  1

typedef enum {
              CSI_MSG_SIZE_32,
              CSI_MSG_SIZE_16,
              CSI_MSG_SIZE_8,
              CSI_MSG_SIZE_1
} CSI_MSG_SIZE;

void csi_write_word(uint32_t addr, uint32_t data);
void csi_write_halfword(uint32_t addr, uint32_t data);
void csi_write_byte(uint32_t addr, uint32_t data);
void csi_write_bit(uint32_t addr, uint32_t data);


uint32_t csi_read_word(uint32_t addr);
uint16_t csi_read_halfword(uint32_t addr);
uint8_t  csi_read_byte(uint32_t addr);
bool     csi_read_bit(uint32_t addr);


bool csi_set_clkDiv(uint16_t div);


// Interrupt functions
static __INLINE void csi_enable_interrupt(void)
{
    // Clear the 'mask' bit to enable the interrupt
    CSI_REGS->rCctl =
        FKB_DIG_CSI_CSR_RCCTL_CIM_MODIFY(CSI_REGS->rCctl, 0u);
}

static __INLINE void csi_disable_interrupt(void)
{
    // setting the mask bit disables propogation of the int flag
    CSI_REGS->rCctl =
        FKB_DIG_CSI_CSR_RCCTL_CIM_MODIFY(CSI_REGS->rCctl, 1u);
}

static __INLINE  bool csi_interrupt_pending(void)
{
    return FKB_DIG_CSI_CSR_RCCTL_CIF_GET(CSI_REGS->rCctl);
}

static __INLINE  void csi_interrupt_clear_pending(void)
{
    // write 0 to the interrupt flag to clear
    CSI_REGS->rCctl =
        FKB_DIG_CSI_CSR_RCCTL_CIF_MODIFY(CSI_REGS->rCctl, 0);
}

// Status register functions
static __INLINE uint32_t csi_get_status(void)
{
    return CSI_REGS->rCgs;
}

static __INLINE bool csi_get_idle(void)
{
    return FKB_DIG_CSI_CSR_RCGS_IDLE_GET(CSI_REGS->rCgs);
}

static __INLINE bool csi_get_error(void)
{
    return FKB_DIG_CSI_CSR_RCGS_ERROR_GET(CSI_REGS->rCgs);
}

static __INLINE uint8_t csi_get_error_num(void)
{
    return FKB_DIG_CSI_CSR_RCGS_ERRORCOUNT_GET(CSI_REGS->rCgs);
}

static __INLINE void csi_clear_errors(void)
{
    CSI_REGS->rCgs = 0;
}

static __INLINE bool csi_set_slave(uint32_t slave)
{
    bool err = true;
    if (csi_get_idle()) {
        CSI_REGS->rCslvsel =
            FKB_DIG_CSI_CSR_RCSLVSEL_BITS_SET(slave);
        err = false;
    }
    return err;
}


#ifdef CSI_INCLUDE_ISR
#include <stdio.h>
extern volatile bool xfer_complete;
void CSI_Handler(void)
{
    // Test only code.
    // Check for the ISR mask, if this is set then we shouldn't be here!
    ASSERT(FKB_DIG_CSI_CSR_RCCTL_CIM_GET(CSI_REGS->rCctl) == 0u);

    // set the transfer complete flag
    xfer_complete = true;
    // ISR remains set until cleared
    csi_interrupt_clear_pending();
    __SEV();
}

#endif
