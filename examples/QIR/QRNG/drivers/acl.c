#include "acl.h"
#include <stdlib.h>
#include <stdio.h>
#include <stdbool.h>
#include "exuberry.h"
/* Used as an event flag for ACL ISR during capture */
volatile bool aclCaptureDone;

void
acl_perform_capture(bool sleep)
{
    ASSERT(acl_get_status() == aclStatusIdle);

    aclCaptureDone = false;

    // trigger the capture
    acl_start_capture();

    if (sleep) {
        while (!aclCaptureDone) {
            // go to sleep and wait for interrupt.
            __WFE();
        }
        ASSERT(acl_get_status() == aclStatusIdle);
    } else {
        // Poll status flag until capture finishes
        while (acl_get_status() != aclStatusDone);
        acl_clear_capture();
    }
}

static uint16_t
config_to_bits(aclConfig cfg)
{
    uint16_t bits = 0;
    switch (cfg) {
    case aclAllEnabled:
        bits = ACL_CFG_ALL;
        break;
    case aclSingleEnable:
        bits = 1 << acl_get_capture_adc();
        break;
    case aclEvenEnable:
        bits = ACL_CFG_EVEN;
        break;
    case aclOddEnable:
        bits = ACL_CFG_ODD;
        break;
    default:
        SEGGER_RTT_WriteString(0, "Invalid config ! \n");
        ASSERT(0);
        break;
    }
    return bits;
}

static int count_enabled_adcs(uint16_t mask)
{
    uint16_t bit;
    int count = 0;
    for (bit = (1 << NUM_ADCS); bit != 0; bit >>= 1) {
            if (mask & bit)
                count++;
    }
    return count;
}

static __INLINE bool
bits_to_enabled(uint16_t bits, int adc)
{
    return bits & (1<<adc);
}

static int16_t
acl_mem_read_sample(int idx)
{
    uint32_t *mem;
    int16_t data = 0;

    int addr_lower;
    int bump = 0;
    mem = (uint32_t*)ACL_BASE;

    /* Work out the index into the memory block for sample N  */
    addr_lower = (idx*NUM_BITS / 32);

    /* This is needed because every 4th memory location is tied off to zero */
    bump = (idx /8);
    addr_lower += bump;

    /* The 12-bit adc samples are packed tight into memory
       We read them out with 32bit accesses.. This pattern
       repeats every 8 samples. 8 * 12 == 96, 96 % 32 == 0
    */
    switch (idx % 8) {
    case 0:
        data = mem[addr_lower] & 0xFFF;
        break;
    case 1:
        data = (mem[addr_lower] >> 12) & 0xFFF;
        break;
    case 2:
        data = ((mem[addr_lower+1] & 0xF) << 8) | (mem[addr_lower] >> 24);
        break;
    case 3:
        data = (mem[addr_lower] >> 4) & 0xFFF;
        break;
    case 4:
        data = (mem[addr_lower] >> 16) & 0xFFF;
        break;
    case 5:
        data = ((mem[addr_lower+1] & 0xFF) << 4) | (mem[addr_lower] >> 28);
        break;
    case 6:
        data = (mem[addr_lower] >> 8) & 0xFFF;
        break;
    case 7:
        data = (mem[addr_lower] >> 20);
        break;
    default:
        break;
    }

    // pad the data up to 16 bits.
    return data;
}

void
acl_read_mem(aclMemResult *res)
{
    unsigned int i, j;
    unsigned int sample_count;

    sample_count = 0;


    // for the entire set of samples
    for (i = 0; i < res->samples; i++) {
        // for each adc
        for (j = 0; j < NUM_ADCS; j++) {
            if (bits_to_enabled(res->cfgEnabled, j)) {
                // read a sample for this block
                ASSERT(res->data[j]);
                res->data[j][i] = acl_mem_read_sample(sample_count);
                sample_count++;
            }
        }
    }
    ASSERT(sample_count == res->totalSamples);
}

bool acl_init_results(aclConfig cfg, uint32_t samples, aclMemResult *res)
{
    int i;
    // keep a bitmask of which ADC's are enabled
    res->cfgEnabled = config_to_bits(cfg);
    res->samples = samples;
    res->num_enabled = count_enabled_adcs(res->cfgEnabled);
    res->totalSamples = samples * res->num_enabled;
    for (i = 0; i < NUM_ADCS; i++) {
        if (bits_to_enabled(res->cfgEnabled, i)) {
            res->data[i] = malloc(sizeof(int16_t) * samples);
            if (res->data[i] == NULL) {
                SEGGER_RTT_WriteString(0, "Failed to allocate acl result\n");
                return false;
            }
        } else {
            res->data[i] = NULL;
        }
    }
    return true;
}
