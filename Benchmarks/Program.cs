OutputEncoding = System.Text.Encoding.UTF8;
WriteLine("Benchmarks for AUSTRA");

Csv csv = new Csv(@"C:\Users\Marteens\Documents\BmeReports\20151216-UAT\CCURVES-EOD.csv")
    .WithSeparator(";")
    .WithFormat("")
    .WithFilter("Curve Name", "EONIA");
DVector vector = new(csv, 15);
Console.WriteLine(vector);

//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
//BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).RunAllJoined();
//BenchmarkRunner.Run<CVectorBenchmark>();
//BenchmarkRunner.Run<SplineBenchmark>();
//BenchmarkRunner.Run<FunctionsBenchmark>();
//BenchmarkRunner.Run<VectorBenchmark>();

