﻿OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
//BenchmarkRunner.Run<CVectorBenchmark>();
//BenchmarkRunner.Run<SplineBenchmark>();
//BenchmarkRunner.Run<FunctionsBenchmark>();
//BenchmarkRunner.Run<VectorBenchmark>();

