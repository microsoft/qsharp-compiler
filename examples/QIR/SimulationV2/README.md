# Building a Simulator for QIR

```shell
clang++ -shared RuntimeManagement.cpp Simulation.cpp -o SimpleSimulator.dll -Iinclude -Llib -l'Microsoft.Quantum.Qir.Runtime' -l'Microsoft.Quantum.Qir.QSharp.Core'
```
