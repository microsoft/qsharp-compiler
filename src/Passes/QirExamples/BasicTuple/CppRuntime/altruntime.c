#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>

int8_t *__quantum__rt__tuple_create(int64_t n)
{
  int8_t * ret = (int8_t *)malloc(n + 3 * sizeof(int64_t));
  int64_t *s   = (int64_t *)(ret);
  int64_t *r   = (int64_t *)(ret + sizeof(int64_t));
  int64_t *a   = (int64_t *)(ret + 2 * sizeof(int64_t));
  *s           = n;
  *r           = 1;
  *a           = 0;
  return ret + 3 * sizeof(int64_t);
}

void __quantum__rt__tuple_update_reference_count(int8_t *tuple, int32_t n)
{
  int8_t * ptr = tuple - 3 * sizeof(int64_t);
  int64_t *r   = (int64_t *)(ptr + sizeof(int64_t));
  *r += (int64_t)(n);

  if (*r <= 0)
  {
    free(ptr);
  }
}