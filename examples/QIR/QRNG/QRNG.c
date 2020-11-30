#include <stdio.h>
#include <stdlib.h>

int PauliX   = 0;
int PauliZ   = 1;
int ResultOne= 1;
int EXE_RESULT[32];
int dummy     =0;

void sleep(int secs) {
    for (int j=0; j<secs; j++)
        for (int i=0; i<2000000; i++)
            dummy += i % 97;
}

int Qrng__RandomBit__body() {
  int   blkPrv = 0;
  int   blkCur = 0;
  int   v[20];
  int*  pv[20];
  while (1) switch (blkCur) {
  case 0:
    ;
                             ////// ['q', '=', 'call', 'Qubit*', '__quantum__rt__qubit_allocate()'] [call]
      int q;
                             ////// ['bases', '=', 'call', 'Array*', '__quantum__rt__array_create_1d(i32', '1', 'i64', '1)'] [call]
      int * pbases = (int*)malloc(sizeof(int)*1);
                             ////// ['v[0]', '=', 'call', 'i8*', '__quantum__rt__array_get_element_ptr_1d(Array*', 'bases', 'i64', '0)'] [call]
      pv[0] = &pbases[0];
                             ////// ['v[1]', '=', 'load', 'i2', 'i2*', 'PauliX'] [load]
      v[1] = PauliX; //load
                             ////// ['v[2]', '=', 'bitcast', 'i8*', 'v[0]', 'to', 'i2*'] [bitcast]
      pv[2] = pv[0]; //bitcast
                             ////// ['store', 'i2', 'v[1]', 'i2*', 'v[2]'] [store]
      v[2] = v[1]; //store
                             ////// ['qubits', '=', 'call', 'Array*', '__quantum__rt__array_create_1d(i32', '8', 'i64', '1)'] [call]
      int * pqubits = (int*)malloc(sizeof(int)*1);
                             ////// ['v[3]', '=', 'call', 'i8*', '__quantum__rt__array_get_element_ptr_1d(Array*', 'qubits', 'i64', '0)'] [call]
      pv[3] = &pqubits[0];
                             ////// ['v[4]', '=', 'bitcast', 'i8*', 'v[3]', 'to', 'Qubit**'] [bitcast]
      pv[4] = pv[3]; //bitcast
                             ////// ['store', 'Qubit*', 'q', 'Qubit**', 'v[4]'] [store]
      v[4] = q; //store
                             ////// ['rslt', '=', 'call', 'Result*', '__quantum__qis__measure(Array*', 'bases', 'Array*', 'qubits)'] [call]
        int rslt = rand() & 0x800 == 0x800 ? 1 : 0;  // Return a random bit
                             ////// ['bases1', '=', 'call', 'Array*', '__quantum__rt__array_create_1d(i32', '1', 'i64', '1)'] [call]
      int * pbases1 = (int*)malloc(sizeof(int)*1);
                             ////// ['v[5]', '=', 'call', 'i8*', '__quantum__rt__array_get_element_ptr_1d(Array*', 'bases1', 'i64', '0)'] [call]
      pv[5] = &pbases1[0];
                             ////// ['v[6]', '=', 'load', 'i2', 'i2*', 'PauliZ'] [load]
      v[6] = PauliZ; //load
                             ////// ['v[7]', '=', 'bitcast', 'i8*', 'v[5]', 'to', 'i2*'] [bitcast]
      pv[7] = pv[5]; //bitcast
                             ////// ['store', 'i2', 'v[6]', 'i2*', 'v[7]'] [store]
      v[7] = v[6]; //store
                             ////// ['qubits2', '=', 'call', 'Array*', '__quantum__rt__array_create_1d(i32', '8', 'i64', '1)'] [call]
      int * pqubits2 = (int*)malloc(sizeof(int)*1);
                             ////// ['v[8]', '=', 'call', 'i8*', '__quantum__rt__array_get_element_ptr_1d(Array*', 'qubits2', 'i64', '0)'] [call]
      pv[8] = &pqubits2[0];
                             ////// ['v[9]', '=', 'bitcast', 'i8*', 'v[8]', 'to', 'Qubit**'] [bitcast]
      pv[9] = pv[8]; //bitcast
                             ////// ['store', 'Qubit*', 'q', 'Qubit**', 'v[9]'] [store]
      v[9] = q; //store
                             ////// ['v[10]', '=', 'call', 'Result*', '__quantum__qis__measure(Array*', 'bases1', 'Array*', 'qubits2)'] [call]
        v[10] = rand() & 0x800 == 0x800 ? 1 : 0;  // Return a random bit
                             ////// ['call', 'void', '__quantum__rt__qubit_release(Qubit*', 'q)'] [call]
                             ////// ['call', 'void', '__quantum__rt__array_unreference(Array*', 'bases)'] [call]
                             ////// ['call', 'void', '__quantum__rt__array_unreference(Array*', 'qubits)'] [call]
                             ////// ['call', 'void', '__quantum__rt__array_unreference(Array*', 'bases1)'] [call]
                             ////// ['call', 'void', '__quantum__rt__array_unreference(Array*', 'qubits2)'] [call]
                             ////// ['ret', 'Result*', 'v[10]'] [ret]
      return v[10];
      break;
  }
}
int Qrng__RandomInt__body() {
  int   blkPrv = 0;
  int   blkCur = 0;
  int   v[20];
  int*  pv[20];
  while (1) switch (blkCur) {
  case 0:
    ;
                             ////// ['rslt', '=', 'alloca', 'i64'] [alloca]
      int rslt; //alloca
                             ////// ['store', 'i64', '0', 'i64*', 'rslt'] [store]
      rslt = 0; //store
                             ////// ['br', 'label', 'header__1'] [br]
      blkPrv = blkCur; blkCur = 1;
      break;
  case 1:
    ;
                             ////// ['i', '=', 'phi', 'i64', '[', 'v[8]', 'continue__1', ']', '[', '0', 'entry', ']'] [phi]
      int i = blkPrv == 2 ? v[8] : 0; //phi
                             ////// ['v[0]', '=', 'icmp', 'sge', 'i64', 'i', '31'] [icmp]
      v[0] = i >= 31 ? 1 : 0; //icmp
                             ////// ['v[1]', '=', 'icmp', 'sle', 'i64', 'i', '31'] [icmp]
      v[1] = i <= 31 ? 1 : 0; //icmp
                             ////// ['v[2]', '=', 'select', 'i1', 'true', 'i1', 'v[1]', 'i1', 'v[0]'] [select]
      v[2] = 1 ? v[1] : v[0]; //select
                             ////// ['br', 'i1', 'v[2]', 'label', 'body__1', 'label', 'exit__1'] [br]
      blkPrv = blkCur; blkCur = v[2] ? 3 : 4;
      break;
  case 3:
    ;
                             ////// ['oneBit', '=', 'call', 'Result*', 'Qrng__RandomBit__body()'] [call]
      int oneBit = Qrng__RandomBit__body();
                             ////// ['v[3]', '=', 'load', 'Result*', 'Result**', 'ResultOne'] [load]
      v[3] = ResultOne; //load
                             ////// ['v[4]', '=', 'call', 'i1', '__quantum__rt__result_equal(Result*', 'oneBit', 'Result*', 'v[3])'] [call]
      v[4] = oneBit == v[3] ? 1 : 0;
                             ////// ['br', 'i1', 'v[4]', 'label', 'then0__1', 'label', 'continue__1'] [br]
      blkPrv = blkCur; blkCur = v[4] ? 5 : 2;
      break;
  case 5:
    ;
                             ////// ['v[5]', '=', 'load', 'i64', 'i64*', 'rslt'] [load]
      v[5] = rslt; //load
                             ////// ['v[6]', '=', 'shl', 'i64', '1', 'i'] [shl]
      v[6] = 1 << i;
                             ////// ['v[7]', '=', 'add', 'i64', 'v[5]', 'v[6]'] [add]
      v[7] = v[5] + v[6]; 
                             ////// ['store', 'i64', 'v[7]', 'i64*', 'rslt'] [store]
      rslt = v[7]; //store
                             ////// ['br', 'label', 'continue__1'] [br]
      blkPrv = blkCur; blkCur = 2;
      break;
  case 2:
    ;
                             ////// ['call', 'void', '__quantum__rt__result_unreference(Result*', 'oneBit)'] [call]
                             ////// ['v[8]', '=', 'add', 'i64', 'i', '1'] [add]
      v[8] = i + 1; 
                             ////// ['br', 'label', 'header__1'] [br]
      blkPrv = blkCur; blkCur = 1;
      break;
  case 4:
    ;
                             ////// ['v[9]', '=', 'load', 'i64', 'i64*', 'rslt'] [load]
      v[9] = rslt; //load
                             ////// ['ret', 'i64', 'v[9]'] [ret]
      return v[9];
      break;
  }
}
int* Qrng__RandomInts__body() {
  int   blkPrv = 0;
  int   blkCur = 0;
  int   v[20];
  int*  pv[20];
  while (1) switch (blkCur) {
  case 0:
    ;
                             ////// ['v[0]', '=', 'call', 'Array*', '__quantum__rt__array_create_1d(i32', '8', 'i64', '32)'] [call]
      pv[0] = (int*)malloc(sizeof(int)*32);
                             ////// ['rslts', '=', 'alloca', 'Array*'] [alloca]
      int* prslts; //alloca
                             ////// ['store', 'Array*', 'v[0]', 'Array**', 'rslts'] [store]
      prslts = pv[0]; //store
                             ////// ['br', 'label', 'header__1'] [br]
      blkPrv = blkCur; blkCur = 1;
      break;
  case 1:
    ;
                             ////// ['i', '=', 'phi', 'i64', '[', 'v[9]', 'body__1', ']', '[', '0', 'entry', ']'] [phi]
      int i = blkPrv == 2 ? v[9] : 0; //phi
                             ////// ['v[1]', '=', 'icmp', 'sge', 'i64', 'i', '31'] [icmp]
      v[1] = i >= 31 ? 1 : 0; //icmp
                             ////// ['v[2]', '=', 'icmp', 'sle', 'i64', 'i', '31'] [icmp]
      v[2] = i <= 31 ? 1 : 0; //icmp
                             ////// ['v[3]', '=', 'select', 'i1', 'true', 'i1', 'v[2]', 'i1', 'v[1]'] [select]
      v[3] = 1 ? v[2] : v[1]; //select
                             ////// ['br', 'i1', 'v[3]', 'label', 'body__1', 'label', 'exit__1'] [br]
      blkPrv = blkCur; blkCur = v[3] ? 2 : 3;
      break;
  case 2:
    ;
                             ////// ['v[4]', '=', 'load', 'Array*', 'Array**', 'rslts'] [load]
      pv[4] = prslts; //load
                             ////// ['v[5]', '=', 'call', 'Array*', '__quantum__rt__array_copy(Array*', 'v[4])'] [call]
      pv[5] = pv[4];
                             ////// ['v[6]', '=', 'call', 'i64', 'Qrng__RandomInt__body()'] [call]
      v[6] = Qrng__RandomInt__body();
                             ////// ['v[7]', '=', 'call', 'i8*', '__quantum__rt__array_get_element_ptr_1d(Array*', 'v[5]', 'i64', 'i)'] [call]
      pv[7] = &pv[5][i];
                             ////// ['v[8]', '=', 'bitcast', 'i8*', 'v[7]', 'to', 'i64*'] [bitcast]
      pv[8] = pv[7]; //bitcast
                             ////// ['store', 'i64', 'v[6]', 'i64*', 'v[8]'] [store]
      *pv[8] = v[6]; //store hack!!!
                             ////// ['store', 'Array*', 'v[5]', 'Array**', 'rslts'] [store]
      prslts = pv[5]; //store
                             ////// ['call', 'void', '__quantum__rt__array_reference(Array*', 'v[5])'] [call]
                             ////// ['call', 'void', '__quantum__rt__array_unreference(Array*', 'v[5])'] [call]
                             ////// ['v[9]', '=', 'add', 'i64', 'i', '1'] [add]
      v[9] = i + 1; 
                             ////// ['br', 'label', 'header__1'] [br]
      blkPrv = blkCur; blkCur = 1;
      break;
  case 3:
    ;
                             ////// ['v[10]', '=', 'load', 'Array*', 'Array**', 'rslts'] [load]
      pv[10] = prslts; //load
                             ////// ['call', 'void', '__quantum__rt__array_unreference(Array*', 'v[0])'] [call]
                             ////// ['ret', 'Array*', 'v[10]'] [ret]
      return pv[10];
      break;
  }
}
int* Qrng_RandomInts() {
  int   blkPrv = 0;
  int   blkCur = 0;
  int   v[20];
  int*  pv[20];
  while (1) switch (blkCur) {
  case 0:
    ;
                             ////// ['v[0]', '=', 'call', 'Array*', 'Qrng__RandomInts__body()'] [call]
      pv[0] = Qrng__RandomInts__body();
                             ////// ['v[1]', '=', 'bitcast', 'Array*', 'v[0]', 'to', '"structquantum::Array"*'] [bitcast]
      pv[1] = pv[0]; //bitcast
                             ////// ['ret', '"structquantum::Array"*', 'v[1]'] [ret]
      return pv[1];
      break;
  }
}

int main() {
    for (int i=0; i<32; i++) EXE_RESULT[i] = -1;
    while (1) {
      sleep(10);
      int* rslt = Qrng_RandomInts();
      for (int i=0; i<32; i++) 
          EXE_RESULT[i] = rslt[i];
  }
}
