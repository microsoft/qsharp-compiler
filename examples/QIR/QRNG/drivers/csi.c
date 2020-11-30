#include "csi.h"
#include <stdio.h>
#include "exuberry.h"
// Used to synch with the ISR when doing reads
volatile bool xfer_complete;

static void
set_data(uint32_t data)
{
    // set the CSI data register
    CSI_REGS->rCdr = FKB_DIG_CSI_CSR_RCDR_DATA_SET(data);
}

static void
set_cmd(bool RnW, CSI_MSG_SIZE size, uint16_t addr)
{
    CSI_REGS->rCcmd =
        FKB_DIG_CSI_CSR_RCCMD_RNW_SET(RnW) |
        FKB_DIG_CSI_CSR_RCCMD_SIZE_SET(size) |
        FKB_DIG_CSI_CSR_RCCMD_REGADDR_SET(addr);
}

static void
csi_write(uint32_t addr, uint32_t data, CSI_MSG_SIZE size)
{
    // wait for pending operations to clear
    while (!csi_get_idle());

    set_data(data);
    set_cmd(CSI_CMD_WRITE, size, addr);
}

void
csi_write_word(uint32_t addr, uint32_t data)
{
    csi_write(addr, data, CSI_MSG_SIZE_32);
}

void
csi_write_halfword(uint32_t addr, uint32_t data)
{
    csi_write(addr, data, CSI_MSG_SIZE_16);
}

void
csi_write_byte(uint32_t addr, uint32_t data)
{
    csi_write(addr, data, CSI_MSG_SIZE_8);
}

void
csi_write_bit(uint32_t addr, uint32_t data)
{
    csi_write(addr, data, CSI_MSG_SIZE_1);
}

static
uint32_t csi_read(uint16_t addr, CSI_MSG_SIZE size)
{
    // wait for any pending operations to clear
    while (!csi_get_idle());

    xfer_complete = false;
    // trigger the read
    set_cmd(CSI_CMD_READ, size, addr);

    // sleep until the read finishes
    while (!xfer_complete) {
        __WFE();
    }

    // make sure the interrupt acted as expected
    ASSERT(csi_get_idle() == true);

    return CSI_REGS->rCdr;
}


uint32_t
csi_read_word(uint32_t addr)
{
    return csi_read(addr, CSI_MSG_SIZE_32);
}

uint16_t
csi_read_halfword(uint32_t addr)
{
    return (uint16_t)csi_read(addr, CSI_MSG_SIZE_16);
}

uint8_t
csi_read_byte(uint32_t addr)
{
    return (uint8_t)csi_read(addr, CSI_MSG_SIZE_8);
}

bool
csi_read_bit(uint32_t addr)
{
    return (bool)csi_read(addr, CSI_MSG_SIZE_1);
}

bool
csi_set_clkDiv(uint16_t div)
{
    bool err = true;
    if (csi_get_idle()) {
        CSI_REGS->rCcfg =
            FKB_DIG_CSI_CSR_RCCFG_CSCLKDIV_SET(div);
        err = false;
    }
    return err;
}

