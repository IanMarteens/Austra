# Austra.Parser
*Austra.Parser* is a layer above *Austra.Library* that implements a simple functional language for accessing the library. We will also call the language _Austra_ for simplicity.

## How to use this package

The best and shorter example about using this package is the [Austra REPL console](https://github.com/IanMarteens/Austra/tree/master/Austra.Repl), available at the Github repository for Austra. It's a very simple console application that reads a command and tries to evaluate it as a formula from the AUSTRA language. When succeeds, the returning values are displayed using their predefined `ToString` methods.

The Austra REPL also allows to define variables and functions, and to load and save them to a JSON file. The JSON file is a simple dictionary that maps variable names to their values. The values are stored as strings, so they can be easily edited by hand.

A more sophisticated WPF application it's also available at the [repository](https://github.com/IanMarteens/Austra/tree/master/Austra), but I suspect most the use cases for the parser will have to do with embedding a formula evaluator with steroids inside another desktop or web application.

## Some examples

This fragment demonstrates how to create a random lower triangular matrix, how to multiply it by its transpose and how to compute its Cholesky decomposition. We also compute the maximum absolute difference between the original matrix and the product of the Cholesky decomposition with its transpose, to check the accuracy of the involved algorithms:
```
let m = matrix::lrandom(5),
    m1 = m * m',
    c = m.chol in
    (c * c' - m).aMax
```

In this simpler example, we generate a vector of 1024 elements, using two harmonic components, and then we compute its Fourier transform, to retrieve the original components:

```
vector::new(1024, i => sin(i*pi/512) + 0.8*cos(i*pi/256)).fft
```

You can also approximate a function in a range using a function and a grid to create the spline:

```
set wave = spline::new(0, 2π, 1024, i => sin(i*π/512) + 0.8*cos(i*π/256))
```

The `set` clause defines a session variable, which stores a non-persistent value. You can use the `wave` variable defined above to interpolate values and first derivative at any point in the defined range:

```
wave[pi / 4]                -- Interpolated value.
wave.derivative(pi / 4)     -- First derivative.
```

## The language

Formulas are parsed and executed in a context defined by a `IDataSource` instance. This context contains variables and definitions than can be initially loaded from a persistent source, such as a database or a JSON file. The current implementation also allows parameterized definitions, that is, real functions. Recursive functions are supported too.

The data source stores two layers of variables. The first one is the session layer, which is volatile and is lost when the session ends. The second one is the persistent layer, which is stored in the data source and is available in future sessions. The persistent layer is read-only, but the session layer can be modified at any time.

For instance, lets say we have a `msft` series with closing prices, stored in the persistent layer. We can define a session variable that shadows it:

```
set msft = msft.rets
```

Now, we have no access to the original series, but we can use the new one, which contains the returns of the original series. We can revert this situation by removing the session variable:

```
set msft
```

## The scanner

Parser and scanner are implemented by a single class, in order to minimize data movement between instances. Access to the scanned text is done using a managed reference to the underlying character array coded as UTF-16. It allows any alphabetic character in identifiers, but case conversion is only supported for ASCII characters. This way, `A` and `a` are considered different characters, but `Π` and `π` are not, which most of the times is the desired behavior.

## The parser
The parser is based on a simple recursive descent algorithm. It generates a .NET expression tree. The process may stop at this point, when we only need to find the type of the expression. Otherwise, the tree is compiled into a delegate, which is finally invoked to evaluate the expression.

At first sight, it seems an overkill to generate a delegate for each expression, but this is not the case, since the language supports lambda expressions.

It is also complicated to force the compile-time evaluation of vectors and matrices, since there are no easy options to store a complex "immediate" value.

The parser performs some optimizations:
* `matrix' * vector` is converted to `matrix.TransposeMultiply(vector)`, saving one operation and one intermediate matrix.
* `matrix1 * matrix2'` is converted to `matrix1.MultiplyTranspose(matrix)`, saving time and space too.
* Both `vector * double + vector1` and `double * vector + vector1` are converted to `vector.MultiplyAdd(double, vector1)`, saving again one operation and one intermediate value.
* `double * vector1 + double * vector2`, and all its variants, are converted to the more efficient `Vector.Combine2(d1, d2, v1, v2    )`. When more than two vectors are involved, it is better to use an overload of the `vector::new` class vector that allows linear combinations directly from the language.
* `matrix * vector1 + vector2` is converted to `matrix.MultiplyAdd(vector1, vector2)`. This time, what we save is a temporal buffer, which also saves some time.
* `matrix * vector1 + double * vector2` is converted to `matrix.MultiplyAdd(vector1, double, vector2)`.

Some simple constant folding also takes place, but it only affects numeric expressions. In any case, most of the time spent by the parser has to do with compiling to IL.

One thing that makes this parser different is the use of synonyms for methods and properties. For example, `i.mag` is equivalent to `i.magnitude`. This is done to make the language more natural to use, but it complicates the parser a bit.

## Experimental features

There are also some experimental features included, like operator elision for multiplications. For example, `2i` is equivalent to `2*i`, and you can write `5x^3 + 3x^2 / 2y` instead of `5*x^3 + 3*x^2 / (2*y)`. This feature only works with numerical variables.

`1 / 2x` is equivalent to `1 / (2*x)`. However, `2x^2` is parsed as `2*(x^2)`, not as `(2*x)^2)`, and `2x.phase` means `2*(x.phase)`, which is the most sensible interpretation. 

Another experimental feature are range comparisons:

```
3 <= pi < 4
pi > e >= 2
```
The only requirement is that both comparison operators must point in the same direction. This feature is better than then usual alternative for ranges: `x in [low, high]`, since it allows for closed or open bounds at both ends of the checked interval.