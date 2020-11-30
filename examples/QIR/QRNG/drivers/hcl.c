#include "hcl.h"
#include <stddef.h>
#include "exuberry.h"

static volatile uint32_t *
getCtrlReg(HeaterNum num)
{
    volatile uint32_t *reg = NULL;
    switch (num) {
    case HEATER0:
        reg = &SRB_REGS->rHtrctl0;
        break;
    case HEATER1:
        reg = &SRB_REGS->rHtrctl1;
        break;
    case HEATER2:
        reg = &SRB_REGS->rHtrctl2;
        break;
    case HEATER3:
        reg = &SRB_REGS->rHtrctl3;
        break;
    default:
        break;
    }
    return reg;
}

void
htr_setSelect(HeaterNum num, uint8_t sel)
{
    volatile uint32_t *htrCtl = getCtrlReg(num);
    *htrCtl = FKB_DIG_SRB_CSR_RHTRCTL0_SEL_MODIFY(*htrCtl, sel);
}

void
htr_setPulseWidth(HeaterNum num, uint8_t width)
{
    volatile uint32_t *htrCtl = getCtrlReg(num);
    *htrCtl = FKB_DIG_SRB_CSR_RHTRCTL0_PULSEWIDTH_MODIFY(*htrCtl, width);
}

void
htr_setDirectMode(HeaterNum num, bool value)
{
    volatile uint32_t *htrCtl = getCtrlReg(num);
    *htrCtl = FKB_DIG_SRB_CSR_RHTRCTL0_DIRECTMODE_MODIFY(*htrCtl, value);
}

void
htr_setSingleShot(HeaterNum num, bool value)
{
    volatile uint32_t *htrCtl = getCtrlReg(num);
    *htrCtl = FKB_DIG_SRB_CSR_RHTRCTL0_SINGLESHOT_MODIFY(*htrCtl, value);
}

void
htr_setStart(HeaterNum num, bool value)
{
    volatile uint32_t *htrCtl = getCtrlReg(num);
    *htrCtl = FKB_DIG_SRB_CSR_RHTRCTL0_START_MODIFY(*htrCtl, value);
}

bool
htr_getStatus(HeaterNum num)
{
    return SRB_REGS->rHtrstatus & (1 << num);
}
