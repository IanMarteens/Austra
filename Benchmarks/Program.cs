OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
BenchmarkRunner.Run<MatrixBenchmark>();
var mb = new MatrixBenchmark();
var v1 = mb.AustraMatrixMultiplyAdd();
var v2 = mb.AustraMatrixMultiplyAddRaw();
WriteLine(v1 - v2);
WriteLine((v1 - v2).AMax());

