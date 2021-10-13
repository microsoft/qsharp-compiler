Replace this file with the [Eigen library](http://eigen.tuxfamily.org/), including the unsupported modules `KroneckerProduct` and `MatrixFunctions`, by copying the following files:

- `Eigen/*` -> `./`
- `unsupported/Eigen/KroneckerProduct` -> `./KroneckerProduct`
- `unsupported/Eigen/src/KroneckerProduct/*` -> `./src/KroneckerProduct/`
- `unsupported/Eigen/MatrixFunctions` -> `./MatrixFunctions`
- `unsupported/Eigen/src/MatrixFunctions/*` -> `./src/MatrixFunctions/`
