OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
//BenchmarkRunner.Run<CVectorBenchmark>();
//BenchmarkRunner.Run<SplineBenchmark>();
//BenchmarkRunner.Run<FunctionsBenchmark>();
BenchmarkRunner.Run<MatrixBenchmark>();

var lm = new LMatrix(16, Random.Shared);
WriteLine(lm.ToString());
WriteLine();
WriteLine(lm.Inverse().ToString());
WriteLine();
WriteLine((lm * lm.Inverse()).ToString());
WriteLine();
WriteLine((lm * lm.Inverse() - LMatrix.Identity(16)).AMax());

