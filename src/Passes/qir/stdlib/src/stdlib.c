#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

typedef void (*CallableSingleFunctor)(int8_t *, int8_t *, int8_t *);

typedef void (*CallableUnknownFunctor)(int8_t *, int32_t);

typedef CallableSingleFunctor  CallableFunctor[4];
typedef CallableUnknownFunctor CallableUnknown[2];

struct Callable
{
  CallableFunctor *functor;
  CallableUnknown *unknown;
  int8_t *         capture;
  int32_t          ref;
  int32_t          alias;
};

int8_t *__quantum__rt__callable_create(CallableFunctor *functor, CallableUnknown *unknown,
                                       int8_t *capture)
{
  struct Callable *ret = (struct Callable *)malloc(sizeof(struct Callable));
  ret->functor         = functor;
  ret->unknown         = unknown;
  ret->capture         = capture;
  ret->ref             = 1;
  ret->alias           = 0;
  return (int8_t *)(ret);
}

void __quantum__rt__callable_update_alias_count(struct Callable *callable, int32_t n)
{
  callable->alias += n;
}

void __quantum__rt__capture_update_alias_count(struct Callable *callable, int32_t n)
{}

void __quantum__rt__capture_update_reference_count(struct Callable *callable, int32_t n)
{}

void __quantum__rt__callable_update_reference_count(struct Callable *callable, int32_t n)
{
  callable->ref += n;
}

void __quantum__rt__callable_invoke(struct Callable *callable, int8_t *args, int8_t *ret)
{
  (*callable->functor)[0](callable->capture, args, ret);
}

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

void __quantum__rt__tuple_update_alias_count(int8_t *tuple, int32_t n)
{
  int8_t * ptr = tuple - 3 * sizeof(int64_t);
  int64_t *a   = (int64_t *)(ptr + 3 * sizeof(int64_t));
  *a += (int64_t)(n);
}

#define QUANTUM_ARRRAY_RESERVED (4 * sizeof(int64_t))

int8_t *__quantum__rt__array_create_1d(int32_t size, int64_t n)
{
  int8_t * ret = (int8_t *)malloc((n * size) + QUANTUM_ARRRAY_RESERVED);
  int64_t *e   = (int64_t *)(ret);
  int64_t *s   = (int64_t *)(ret + sizeof(int64_t));
  int64_t *r   = (int64_t *)(ret + 2 * sizeof(int64_t));
  int64_t *a   = (int64_t *)(ret + 3 * sizeof(int64_t));
  *e           = (int64_t)size;
  *s           = n;
  *r           = 1;
  *a           = 0;
  return ret;
}

struct Range
{
  int64_t a;
  int64_t b;
  int64_t c;
};

int8_t *__quantum__rt__array_concatenate(int8_t *array1, int8_t *array2)
{
  int64_t e  = *(int64_t *)(array1);
  int64_t s1 = *(int64_t *)(array1 + sizeof(int64_t));

  int64_t s2 = *(int64_t *)(array2 + sizeof(int64_t));
  array1 += QUANTUM_ARRRAY_RESERVED;
  array2 += QUANTUM_ARRRAY_RESERVED;

  int8_t *ret       = __quantum__rt__array_create_1d(e, s1 + s2);
  int8_t *ret_array = ret + QUANTUM_ARRRAY_RESERVED;

  memcpy(ret_array, array1, s1 * e);
  memcpy(ret_array + s1 * e, array2, s2 * e);

  return ret;
}

int8_t *__quantum__rt__array_copy(int8_t *array, int8_t force)
{
  if (array == NULL)
  {
    return NULL;
  }

  int64_t *e = (int64_t *)(array);
  int64_t *s = (int64_t *)(array + sizeof(int64_t));

  int64_t *a = (int64_t *)(array + 3 * sizeof(int64_t));

  if (force || *a > 0)
  {
    int8_t *ret = __quantum__rt__array_create_1d(*e, *s);
    memcpy(ret, array, (*e) * (*s));
    return ret;
  }

  return array;
}

int64_t __quantum__rt__array_get_size_1d(int8_t *array)
{
  int64_t *s = (int64_t *)(array + sizeof(int64_t));
  return *s;
}

int8_t *__quantum__rt__array_get_element_ptr_1d(int8_t *array, int64_t n)
{
  int64_t *e = (int64_t *)(array);
  return (array + (*e) * n + QUANTUM_ARRRAY_RESERVED);
}

void __quantum__rt__array_update_alias_count(int8_t *arr, int32_t n)
{
  int64_t *a = (int64_t *)(arr + 3 * sizeof(int64_t));
  *a += (int64_t)(n);
}

void __quantum__rt__array_update_reference_count(int8_t *arr, int32_t n)
{
  int64_t *r = (int64_t *)(arr + 2 * sizeof(int64_t));
  *r += (int64_t)(n);

  if (*r <= 0)
  {
    free(arr);
  }
}
