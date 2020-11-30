#pragma once
#include <stdint.h>
#include <ARMCM4_FP.h>


typedef struct
{
    __IO uint32_t SWPORTA_DR;
    __IO uint32_t SWPORTA_DDR;
    __IO uint32_t SWPORTA_CTL;
    __IO uint32_t SWPORTB_DR;
    __IO uint32_t SWPORTB_DDR;
    __IO uint32_t SWPORTB_CTL;
    __IO uint32_t SWPORTC_DR;
    __IO uint32_t SWPORTC_DDR;
    __IO uint32_t SWPORTC_CTL;
    __IO uint32_t SWPORTD_DR;
    __IO uint32_t SWPORTD_DDR;
    __IO uint32_t SWPORTD_CTL;
    __IO uint32_t INTEN;
    __IO uint32_t INTMASK;
    __IO uint32_t INTTYPE_LEVEL;
    __IO uint32_t INT_POLARITY;
    __I  uint32_t INTSTATUS;
    __I  uint32_t RAW_INTSTATUS;
    __IO uint32_t DEBOUNCE;
    __O  uint32_t PORTA_EOI;
    __I  uint32_t SWPORTA_EXT;
    // others
} GPIO_t;

#define GPIO_BASE ((uint32_t )0x51000000)
#define GPIO0     ((GPIO_t*) GPIO_BASE)




#define SET_BIT(BIT)                            \
    do {                                        \
        GPIO0->SWPORTA_DR |= (1 << (BIT));      \
    } while(0)                                  \

#define CLEAR_BIT(BIT)                          \
    do {                                        \
        GPIO0->SWPORTA_DR &= (~(1 << (BIT)));   \
    } while(0)                                  \

#define gpio_mcu_gpio0_init()                                       \
    do {                                                            \
        GPIO0->SWPORTA_DR = 0; /* clear all outputs */              \
        GPIO0->SWPORTA_DDR = /* set the required pins as outputs */ \
            (1 << CHAR_STROBE)             |                        \
            (0x7F << GPIO_PUTCH)           |                        \
            (0x1f << GPIO_DD_FUNCSEL)      |                        \
            (1    << GPIO_DD_FUNCSEL_STRB) |                        \
            (1 << GPIO_TEST_PASS)          |                        \
            (1 << TEST_COMPLETE);                                   \
        GPIO0->INT_POLARITY = 0xFFFFFFFF;/*Active High interrupts*/ \
    } while(0)
