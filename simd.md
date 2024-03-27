# Austra SIMD Guide

Most of the code related to SIMD instructions has been grouped inside the [Helpers folder](https://github.com/IanMarteens/Austra/tree/master/Austra.Library/Helpers) of the library.

These are the relevant classes:

* [Simd](https://github.com/IanMarteens/Austra/blob/master/Austra.Library/Helpers/Simd.cs): Implements functions using AVX256 and AVX512 instructions.
* [RandomAvx](https://github.com/IanMarteens/Austra/blob/master/Austra.Library/Helpers/RandomAvx.cs): Implements random number generation using AVX256 and AVX512 instructions, both for a uniform distribution and a normal distribution.
* [Vec](https://github.com/IanMarteens/Austra/blob/master/Austra.Library/Helpers/Vec.cs): A static classes containing generic and non-generic functions to work with arrays and spans.

## Simd