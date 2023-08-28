Console.WriteLine("Benchmarks for AUSTRA");

//BenchmarkRunner.Run<VectorBenchmark>();
//BenchmarkRunner.Run<MatrixBenchmark>();
//BenchmarkRunner.Run<EvdBenchmark>();
//BenchmarkRunner.Run<LuBenchmark>();
//BenchmarkRunner.Run<CholeskyBenchmark>();
//BenchmarkRunner.Run<FftBenchmark>();
//BenchmarkRunner.Run<SeriesBenchmark>();
//BenchmarkRunner.Run<ParserBenchmark>();
BenchmarkRunner.Run<SplineBenchmark>();

//var p = new ParserBenchmark();
//for (int i = 0; i < 1_000_000; i++)
//{
//    p.AustraParseSimpleSum();
//    p.AustraParseMatrixTrace();
//}
//Console.Write("> "); Console.ReadLine();
//for (int i = 0; i < 5_000_000; i++)
//{
//    p.AustraParseSimpleSum();
//    p.AustraParseMatrixTrace();
//}
//Console.WriteLine("Austra parse simple sum: " + p.AustraParseSimpleSum());
//Console.WriteLine("Austra parse matrix trc: " + p.AustraParseMatrixTrace());

//EvdBenchmark.Warm();
//Console.Write("> "); Console.ReadLine();
//new EvdBenchmark().EvdAustraMatrix();
//return;
//FftBenchmark.Warm();
//FftBenchmark.Trace();
//EvdBenchmark.Trace(17);
//new LuBenchmark().LuAustraMatrix();
