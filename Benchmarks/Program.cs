OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
//BenchmarkRunner.Run<CVectorBenchmark>();
//BenchmarkRunner.Run<SplineBenchmark>();
//BenchmarkRunner.Run<FunctionsBenchmark>();
//BenchmarkRunner.Run<MatrixBenchmark>();

var rm = new RMatrix(5, 5, [1, 2, 3, 4, 5, 0, 6, 7, 8, 9, 0, 0, 10, 11, 12, 0, 0, 0, 13, 14, 0, 0, 0, 0, 15]);
WriteLine(rm.ToString());
WriteLine(rm.Inverse().ToString());
WriteLine((rm * rm.Inverse()).ToString());
