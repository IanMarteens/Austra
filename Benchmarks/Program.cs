﻿OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
WriteLine(new ParserBenchmark().AustraGamma());
BenchmarkRunner.Run<ParserBenchmark>();


