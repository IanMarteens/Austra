using System.Diagnostics;

OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
//BenchmarkRunner.Run<ParserBenchmark>();

//MatrixBenchmark mb = new();
//WriteLine("Warming up...");
//for (int i = 0; i < 10_000; i++)
//    mb.AustraMulMatrix();
//WriteLine("1");
//for (int i = 0; i < 10_000; i++)
//    mb.AustraMulMatrix();
//WriteLine("2");
//for (int i = 0; i < 20_000; i++)
//    mb.AustraMulMatrix();
//WriteLine("Running benchmark...");
//var sw = Stopwatch.StartNew();
//for (int i = 0; i < 50_000; i++)
//    mb.AustraMulMatrix();
//WriteLine("1");
//for (int i = 0; i < 50_000; i++)
//    mb.AustraMulMatrix();
//WriteLine("2");
//for (int i = 0; i < 100_000; i++)
//    mb.AustraMulMatrix();
//sw.Stop();
//WriteLine($"Elapsed: {sw.ElapsedMilliseconds} ms");


