OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

BenchmarkRunner.Run<VectorBenchmark>();
BenchmarkRunner.Run<MatrixBenchmark>();
BenchmarkRunner.Run<EvdBenchmark>();
BenchmarkRunner.Run<LuBenchmark>();
BenchmarkRunner.Run<CholeskyBenchmark>();
BenchmarkRunner.Run<FftBenchmark>();
BenchmarkRunner.Run<SeriesBenchmark>();
BenchmarkRunner.Run<ParserBenchmark>();
BenchmarkRunner.Run<SplineBenchmark>();
