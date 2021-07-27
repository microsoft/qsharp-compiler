#include <vector>

void QuantumFunction(int32_t nQubits)
{
  volatile uint64_t x = 3;
  for (uint64_t i = 0; i < x; ++i)
  {
    nQubits += nQubits;
  }
  int32_t qubits[nQubits];
}

int main()
{
  QuantumFunction(10);
  QuantumFunction(3);
  return 0;
}