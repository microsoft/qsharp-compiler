#include <cstdint>
#include <unordered_map>

class Array
{
public:
  Array(int64_t size_, int64_t count_)
    : size{std::move(size_)}
    , count{std::move(count_)}
  {
    data = new int8_t[size * count];
  }

  Array(Array *ref)
    : size{ref->size}
    , count{ref->count}
  {
    data = new int8_t[size * count];
    memcpy(ref->data, data, size * count);
  }

  ~Array()
  {
    delete[] data;
  }
  int64_t size;
  int64_t count;
  int8_t *data;
};

std::unordered_map<Array *, int32_t> alias_count;
std::unordered_map<Array *, int32_t> ref_count;

extern "C"
{

  Array *__quantum__rt__array_create_1d(int32_t size, int64_t n)
  {
    return new Array(size, n);
  }

  int8_t *__quantum__rt__array_get_element_ptr_1d(Array *array, int64_t n)
  {
    return array->data + n * array->size;
  }

  void __quantum__rt__qubit_release_array(Array *array)
  {
    delete array;
  }

  void __quantum__rt__array_update_alias_count(Array *arr, int32_t n)
  {
    alias_count[arr] += n;
  }

  void __quantum__rt__array_update_reference_count(Array *arr, int32_t n)
  {
    ref_count[arr] += n;
  }

  Array *__quantum__rt__array_copy(Array *arr, bool force)
  {
    if (arr == nullptr)
    {
      return nullptr;
    }

    if (force || alias_count[arr] > 0)
    {
      return new Array(arr);
    }

    return arr;
  }
}