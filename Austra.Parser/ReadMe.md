# Austra.Parser
*Austra.Parser* is a layer above *Austra.Library* that implements a simple functional language for accessing the library.

## Examples

This fragment demonstrates how to create a random lower triangular matrix, how to multiply it by its transpose and how to compute its Cholesky decomposition. Then, we compute the maximum absolute difference between the original matrix and the product of the Cholesky decomposition with its transpose, to check the accuracy of the involved algorithms:
```
let m = matrix::lrandom(5),
    m1 = m * m',
    c = m.chol in
    (c * c' - m).aMax
```

In this simpler example, we generate a vector of 1024 elements, using two harmonic components, and the compute its Fourier transform, to retrieve the original components:

```
vector::new(1024, i => sin(i*pi/512) + 0.8*cos(i*pi/256)).fft
```

## The parser
The parser is based on a simple recursive descent algorithm. It generates a .NET expression tree that is then compiled into a delegate. The delegate is then invoked to evaluate the expression.

At first sight, it seems an overkill to generate a delegate for each expression, but this is not the case, since the language supports lambda expressions.

The parser is also able to perform some optimizations:
* `matrix' * vector` is converted to `matrix.TransposeMultiply(vector)`, saving one operation and one intermediate matrix.
* `matrix1 * matrix2'` is converted to `matrix1.MultiplyTranspose(matrix)`, saving time and space too.
* Both `vector * double + vector1` and `double * vector + vector1` are converted to `vector.MultiplyAdd(double, vector1)`, saving again one operation and one intermediate value.
* `matrix * vector1 + vector2` is converted to `matrix.MultiplyAdd(vector1, vector2)`. This time, what we save is a temporal buffer, which also saves some time.

Some simple constant folding also takes place, but it only affects numeric expressions. In any case, most of the time spent by the parser has to do with compiling to IL.

One thing that makes this parser different is the use of synonyms for methods and properties. For example, `i.mag` is equivalent to `i.magnitude`. This is done to make the language more natural to use, but complicates the parser a bit.

There are also some experimental features, like operator elision for multiplications. For example, `2i` is equivalent to `2*i`, and you can write `5x^3 + 3x^2 / 2y` instead of `5*x^3 + 3*x^2 / (2*y)`. This feature only works with numerical variables.
