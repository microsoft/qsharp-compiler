#include <cstdint>
#include <unordered_map>

class Array
{
public:
  Array(int64_t size_, int64_t count_) noexcept
    : size{std::move(size_)}
    , count{std::move(count_)}
  {
    data = new int8_t[size * count];
  }

  Array(Array *ref) noexcept
    : size{ref->size}
    , count{ref->count}
  {
    data = new int8_t[size * count];
    memcpy(ref->data, data, size * count);
  }

  ~Array() noexcept
  {
    delete[] data;
  }

  int64_t size;
  int64_t count;
  int8_t *data;
  int64_t alias_count{0};
  int64_t ref_count{1};
};

extern "C"
{
  int8_t *__quantum__rt__tuple_create(int64_t n)
  {
    auto  ret = new int8_t[n + 3 * sizeof(int64_t)];
    auto &s   = *reinterpret_cast<int64_t *>(ret);
    auto &r   = *reinterpret_cast<int64_t *>(ret + sizeof(int64_t));
    auto &a   = *reinterpret_cast<int64_t *>(ret + 2 * sizeof(int64_t));
    s         = n;
    r         = 1;
    a         = 0;
    return ret + 3 * sizeof(int64_t);
  }

  void __quantum__rt__tuple_update_reference_count(int8_t *tuple, int32_t n)
  {
    auto  ptr = tuple - 3 * sizeof(int64_t);
    auto &r   = *reinterpret_cast<int64_t *>(ptr + sizeof(int64_t));
    r += static_cast<int64_t>(n);
    if (r <= 0)
    {
      delete ptr;
    }
  }

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
    arr->alias_count += n;
  }

  void __quantum__rt__array_update_reference_count(Array *arr, int32_t n)
  {
    arr->ref_count += n;
  }

  Array *__quantum__rt__array_copy(Array *arr, bool force)
  {
    if (arr == nullptr)
    {
      return nullptr;
    }

    if (force || arr->alias_count > 0)
    {
      //      return arr;
      return new Array(arr);
    }

    return arr;
  }
}