# Austra

*Austra* seems to be another spelling for *Ostara* or *Ēostre*, a West Germanic spring goddess. It is also a catchy name for a software library, but I may be wrong on this.

It has two main components:
* Austra.Library
* Austra.Parser

The library has been designed both for direct use and for using it through a simple formula-oriented language.

The Austra library deals with common numeric algorithms, such as vector and matrix operations, efficient dates, Discrete Fourier Transform, and even a Mean Variance Optimizer. It also supports time series, linear regressions, AR models and sundry other things.

## Why Austra?

Why yet another numeric library?
* The managed implementation has been __thoroughly tested and optimized__.
* Even if you will not use Austra in your code, the repository is __an excellent cheat sheet__ for numeric algorithms and AVX/AVX2 optimizations. In fact, I am currently writing a book (Unsafe C#, as provisional title) on the techniques commonly used for numeric computations in .NET/C#.
* Austra targets a useful __sweet spot__. I started writing this library when dealing with a financial application that used squared matrices in the order of 32-128 rows and columns. In this range of problems, AVX + pointers provide the faster solutions, without losing precision, and without needing any third-party low-level provider.
* Austra is a library _plus_ a language. It is easier to try the functionality through the language, and then use the library directly in your code.

More sophisticated use cases are also supported. For instance, your final customer may need customizable expressions in an application. Austra can then be used as a __scripting engine__ for your application, with a quite simple syntax and little overhead.

## How to give the language a try

There is a Big Bad Austra Desktop application, written in WPF, with all the required bells and whistles... but it uses third-party controls, so I cannot open source it.

Meanwhile, you can try the language through the Austra.Console application. It's a simple console application that reads a formula at a time, evaluates it, and then prints the results to the console.

## Acknowledgments

I have used two libraries as a reference, and as a source of inspiration, for developing Austra: [AlgLib](https://www.alglib.net/) and [Math.NET](https://numerics.mathdotnet.com/). Regarding the algorithms, my main source of information has been these books:
* Time Series Analysis, Forecasting and Control, by George Box, Gwilym Jenkins and Gregory Reinsel* Linear Algebra, by J.H. Wilkinson and C. Reinsch
* Matrix Mathematics, by S.R. Garcia and Roger A. Horn

Last, but not least, the main resource for AVX/AVX2 optimizations has been the excellent [Agner Fog's manuals](https://www.agner.org/optimize/).

## Documentation

A PDF manual, covering the language, is available [here](https://marteens.com/austra/austra.pdf). An online help version of the same manual is [also available](https://marteens.com/austra/).

We have plans to create a full online help for Visual Studio using Sandcastle soon.

## Next steps

This project is still in an early stage of development. Most compelling needs are, not necessarily in that order:

* Adding a `seq` class for doing calculations that, right now, are only possible when using vectors... and consuming space.
* The parser should use a more flexible way to match arguments versus parameters. Right now, it is a very ad hoc algorithm, full of exceptional cases.
* The lexical scanner must be rewritten. I'm sure there's a lot of index checking going on in the dark. But I cannot use spans, or pointers, or managed references, or whatever as long as the scanner is programmed as an iterator.
* A reasonably good desktop application is also needed.
* Both the REPL and any desktop app, should have control on formatting output and sending it to external files (Excel, JSON, CSV). It could be interesting formatting some kind of outputs to C# format.
* Parameterized definitions, that is, actual functions.
* Connectors, to access external data sources with real time series, such as stock prices, meteorological data, etc.

In fact, we already have code for storing series in SQL Server, and it will be added to the repository in the near future, after some cleaning.

The library, of course, must also be expanded:

* A decent implementation of ARIMA. We already implement AR, but not MA.
* GARCH, EGARCH and variants.
* More matrix decompositions are crucial.
* Complex matrices.
* More transforms.
* A good simplex implementation could be nice.
* We will probably need to add support for 64 bits integers.
* Optional task-based concurrency for big matrices and vectors.
* Sparse matrices.
* It can be useful to allow date homogenization for time series, both by adjusting common ranges and by interpolating missing values.

Any suggestions will be welcomed.

Feel free to use the code as you need.