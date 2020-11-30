#pragma once

#include "exuberry.h"
#include <stdint.h>
#include <stdbool.h>

typedef enum {
              HEATER0,
              HEATER1,
              HEATER2,
              HEATER3
} HeaterNum;

void htr_setSelect(HeaterNum num, uint8_t sel);

void htr_setPulseWidth(HeaterNum num, uint8_t width);

void htr_setDirectMode(HeaterNum num, bool value);

void htr_setSingleShot(HeaterNum num, bool value);

void htr_setStart(HeaterNum num, bool value);

bool htr_getStatus(HeaterNum num);
