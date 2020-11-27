int PauliX   = 0;
int PauliZ   = 1;
int ResultOne= 1;
int* EXE_RESULT;

int Qrng__RandomBit__body() {
  int blkPrv = 0;
  int blkCur = 0;
  while (1) switch (blkCur) {
  case 0:
      int* v_q = 0;
      int v_bases[1];
      int* v_0 = v_bases;
      int v_1 = PauliX; //load
      int* v_2 = v_0; //bitcast
      v_2 = v_1; //store
      int v_qubits[1];
      int* v_3 = v_qubits;
      int* v_4 = v_3; //bitcast
      v_4 = v_q; //store
      int v_rslt = 1;
      int v_bases1[1];
      int* v_5 = v_bases1;
      int v_6 = PauliZ; //load
      int* v_7 = v_5; //bitcast
      v_7 = v_6; //store
      int v_qubits2[1];
      int* v_8 = v_qubits2;
      int* v_9 = v_8; //bitcast
      v_9 = v_q; //store
      int v_10 = 1;
      return v_10;
      break;
  }
}
int Qrng__RandomInt__body() {
  int blkPrv = 0;
  int blkCur = 0;
  while (1) switch (blkCur) {
  case 0:
      int v_rslt; //alloca
      v_rslt = 0; //store
      blkPrv = blkCur; blkCur = 1;
      break;
  case 1:
      int v_i = blkPrv == 2 ? v_8 : 0; //phi
      int v_0 = v_i >= 31 ? 1 : 0; //icmp
      int v_1 = v_i <= 31 ? 1 : 0; //icmp
      int v_2 = 1 ? v_1 : v_0; //select
      blkPrv = blkCur; blkCur = v_2 ? 3 : 4;
      break;
  case 3:
      int v_oneBit = Qrng__RandomBit__body();
      int v_3 = ResultOne; //load
      int v_4 = v_3;
      blkPrv = blkCur; blkCur = v_4 ? 5 : 2;
      break;
  case 5:
      int v_5 = v_rslt; //load
      int v_6 = v_5 + 1; 
      int v_7 = v_6 << v_i;
      v_rslt = v_7; //store
      blkPrv = blkCur; blkCur = 2;
      break;
  case 2:
      int v_8 = v_i + 1; 
      blkPrv = blkCur; blkCur = 1;
      break;
  case 4:
      int v_9 = v_rslt; //load
      return v_9;
      break;
  }
}
int* Qrng__RandomInts__body() {
  int blkPrv = 0;
  int blkCur = 0;
  while (1) switch (blkCur) {
  case 0:
      int v_0[32];
      int* v_rslts; //alloca
      v_rslts = v_0; //store
      blkPrv = blkCur; blkCur = 1;
      break;
  case 1:
      int v_i = blkPrv == 2 ? v_9 : 0; //phi
      int v_1 = v_i >= 31 ? 1 : 0; //icmp
      int v_2 = v_i <= 31 ? 1 : 0; //icmp
      int v_3 = 1 ? v_2 : v_1; //select
      blkPrv = blkCur; blkCur = v_3 ? 2 : 3;
      break;
  case 2:
      int v_4 = v_rslts; //load
      int* v_5 = v_4;
      int v_6 = Qrng__RandomInt__body();
      int* v_7 = v_5;
      int* v_8 = v_7; //bitcast
      v_8 = v_6; //store
      v_rslts = v_5; //store
      int v_9 = v_i + 1; 
      blkPrv = blkCur; blkCur = 1;
      break;
  case 3:
      int v_10 = v_rslts; //load
      return v_10;
      break;
  }
}
int* Qrng_RandomInts() {
  int blkPrv = 0;
  int blkCur = 0;
  while (1) switch (blkCur) {
  case 0:
      int v_0 = Qrng__RandomInts__body();
      int* v_1 = v_0; //bitcast
      return v_1;
      break;
  }
}

int main() {
    EXE_RESULT = Qrng_RandomInts();
}
