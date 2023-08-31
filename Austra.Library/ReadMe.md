# Austra.Library
*Austra.Library* is a library for .NET Core 7 that provides classes for linear algebra, statistics, time series and transforms. All classes are totally implemented using managed code, but implementations are optimized by using low-level C# features such as pointers and hardware intrinsics.

## Linear Algebra
Austra provides classes for dense vectors and matrices, for double-precision arithmetic. It also features an efficient `ComplexVector` class. Single-precision floats, complex and sparse matrices are planned for a future sprint. All operations takes advantage of C# operators when possible, so most of the operations are non-destructive.

There are three classes for representing matrices:
* `Matrix` is the general class that you will use most of the time.
* Lower triangular matrices are represented by the `LMatrix` class.
* Upper triangular matrices are represented by the `RMatrix` class.

The point with this two additional classes is not to save space, since the underlying data structure is the same, but to provide a more efficient implementation of several methods and operators. There's also some logical advantages, regarding type safety, since some decompositions returns triangular matrices.

As usual, matrix multiplication has been fully optimized using loop reordering and unrolling, blocking and hardware intrinsics, including fused multiply and add. There are variants for multiplying a matrix by another matrix transposed on-the-fly, for multiplying a vector by a transposed matrix and for accelerating linear combinations of vectors.

## Matrix Decompositions
Austra provides classes for the following matrix decompositions:
* Lower-Upper (LU) Decomposition
* Cholesky Decomposition
* Eigenvalues Decomposition (EVD)

`Matrix.Solve(Vector)` and `Matrix.Solve(Matrix)` uses LU decomposition internally.

## Time series

The kernel of Austra was our implementation of the Mean-Variance optimizer. This means that time series were implemented before vectors and matrices.

Series are collections of pairs date/value, and they are sorted by date. Values can be used as vectors, but there are some differences. Vector operations check, at run time, that the operands have the same length. The same behavior would be hard to enforce for series. On one hand, each series can have a different first available date. On the other hand, even series with the same frequency could have reported values at different days of the week or the month, and still, it could be interesting to mix them.

So, the rules for mixing two series in an operation are:

* They must have the same frequency, and their frequencies are checked at runtime.
* However, they may have different lengths. If this is the case, the shorter length is chosen for the result.
* The points of the series are aligned according to their most recent points.
* The list of dates assigned to the result series is chosen arbitrarily from the first operand.

## Polynomials and root finding

The `Polynomials` static class provides methods for polynomial evaluation and root finding. The `Solver` class implements a simple variant of the Newton-Raphson method for root finding.

There's also a `PolyEval` for evaluating polynomials using the Horner's method, and a `PolySolve` for analytically finding roots whenever possible.

## Fast Fourier Transform
Austra implements a pretty decent FFT algorithm, compared to most popular managed implementations. It uses the Cooley-Tukey algorithm, and it's optimized for small sizes. Small primes are handled either with Bluestein's or Rader's algorithm, depending on the size.

In any case, there is still room for improvement, and it's planned to be optimized in the future. AVX prefers structs of arrays over arrays of structures, and this preference obviously applies to complex arithmetic: it's more efficient to represent the real and the imaginary parts of a list of complex numbers in separate arrays.
