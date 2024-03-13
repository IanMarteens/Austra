OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
//BenchmarkRunner.Run<CVectorBenchmark>();
//BenchmarkRunner.Run<SplineBenchmark>();
//BenchmarkRunner.Run<FunctionsBenchmark>();
BenchmarkRunner.Run<MatrixBenchmark>();

var lm = new LMatrix(128, 128, Random.Shared, 0.3, 1.1);
WriteLine(lm.ToString());
WriteLine();
WriteLine(lm.Inverse().ToString());
WriteLine();
WriteLine((lm * lm.Inverse()).ToString());
WriteLine();
WriteLine((lm * lm.Inverse() - LMatrix.Identity(128)).AMax());
WriteLine((Matrix.Identity(128) / lm * lm - Matrix.Identity(128)).AMax());
