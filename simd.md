# Austra SIMD Guide

Most of the code related to SIMD instructions has been grouped inside the [Helpers folder](https://github.com/IanMarteens/Austra/tree/master/Austra.Library/Helpers) of the library.

These are the relevant classes:

* [Simd](https://github.com/IanMarteens/Austra/blob/master/Austra.Library/Helpers/Simd.cs): Implements functions using AVX256 and AVX512 instructions.
* [RandomAvx](https://github.com/IanMarteens/Austra/blob/master/Austra.Library/Helpers/RandomAvx.cs): Implements random number generation using AVX256 and AVX512 instructions, both for a uniform distribution and a normal distribution.
* [Vec](https://github.com/IanMarteens/Austra/blob/master/Austra.Library/Helpers/Vec.cs): A static class containing generic and non-generic functions to work with arrays and spans.

## Vec

The `Vec` class contains functions that can be used to perform operations on arrays and spans, such as addition, multiplication, etc.
It also contains functions to perform operations on vectors of different sizes, such as 256-bit and 512-bit vectors.

Most of these functions are internal to the package. .NET has rendered some of the original functions obsolete, by widening the scope of the `System.Runtime.Intrinsics` namespace,
which now contains functions that can be used to perform operations on vectors of different sizes, such as 256-bit and 512-bit vectors.