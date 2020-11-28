#include <stdio.h>
#include <stdlib.h>
#include <time.h>
int PauliX   = 0;
int PauliZ   = 1;
int ResultOne= 1;
int* EXE_RESULT;

int Qrng__RandomBit__body() {
  int   blkPrv = 0;
  int   blkCur = 0;
  int   v[20];
  int*  pv[20];
  while (1) switch (blkCur) {
  case 0:
    ;

      int q;
      int * pbases = (int*)malloc(sizeof(int)*1);
      pv[0] = &pbases[0];
      v[1] = PauliX; //load
      pv[2] = pv[0]; //bitcast
      v[2] = v[1]; //store
      int * pqubits = (int*)malloc(sizeof(int)*1);
      pv[3] = &pqubits[0];
      pv[4] = pv[3]; //bitcast
      v[4] = q; //store
      int rslt = rand() & 0x800 == 0x800 ? 1 : 0;  // Return a random bit
      int * pbases1 = (int*)malloc(sizeof(int)*1);
      pv[5] = &pbases1[0];
      v[6] = PauliZ; //load
      pv[7] = pv[5]; //bitcast
      v[7] = v[6]; //store
      int * pqubits2 = (int*)malloc(sizeof(int)*1);
      pv[8] = &pqubits2[0];
      pv[9] = pv[8]; //bitcast
      v[9] = q; //store
      v[10] = rand() & 0x800 == 0x800 ? 1 : 0;  // Return a random bit
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
      int rslt; //alloca
      rslt = 0; //store
      blkPrv = blkCur; blkCur = 1;
      break;
  case 1:
    ;
      int i = blkPrv == 2 ? v[8] : 0; //phi
      v[0] = i >= 31 ? 1 : 0; //icmp
      v[1] = i <= 31 ? 1 : 0; //icmp
      v[2] = 1 ? v[1] : v[0]; //select
      blkPrv = blkCur; blkCur = v[2] ? 3 : 4;
      break;
  case 3:
    ;
      int oneBit = Qrng__RandomBit__body();
      v[3] = ResultOne; //load
      v[4] = oneBit == v[3] ? 1 : 0;
      blkPrv = blkCur; blkCur = v[4] ? 5 : 2;
      break;
  case 5:
    ;
      v[5] = rslt; //load
      v[6] = 1 << i;
      v[7] = v[5] + v[6]; 
      rslt = v[7]; //store
      blkPrv = blkCur; blkCur = 2;
      break;
  case 2:
    ;
      v[8] = i + 1; 
      blkPrv = blkCur; blkCur = 1;
      break;
  case 4:
    ;
      v[9] = rslt; //load
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
      pv[0] = (int*)malloc(sizeof(int)*32);
      int* prslts; //alloca
      prslts = pv[0]; //store
      blkPrv = blkCur; blkCur = 1;
      break;
  case 1:
    ;
      int i = blkPrv == 2 ? v[9] : 0; //phi
      v[1] = i >= 31 ? 1 : 0; //icmp
      v[2] = i <= 31 ? 1 : 0; //icmp
      v[3] = 1 ? v[2] : v[1]; //select
      blkPrv = blkCur; blkCur = v[3] ? 2 : 3;
      break;
  case 2:
    ;
      pv[4] = prslts; //load
      pv[5] = pv[4];
      v[6] = Qrng__RandomInt__body();
      pv[7] = &pv[5][i];
      pv[8] = pv[7]; //bitcast
      *pv[8] = v[6]; //store hack!!!
      prslts = pv[5]; //store
      v[9] = i + 1; 
      blkPrv = blkCur; blkCur = 1;
      break;
  case 3:
    ;
      pv[10] = prslts; //load
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
      pv[0] = Qrng__RandomInts__body();
      pv[1] = pv[0]; //bitcast
      return pv[1];
      break;
  }
}

int main() {
    srand(time(NULL));
    EXE_RESULT = Qrng_RandomInts();
    for (int i=0; i<32; i++) 
        printf("%2d = %08x\n",i,EXE_RESULT[i]);
}
