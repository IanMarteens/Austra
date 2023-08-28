using Austra.Parser;

namespace Benchmarks;

public class ParserBenchmark : BenchmarkControl
{
    private readonly IDataSource dataSource;
    private readonly IAustraEngine engine;

    public ParserBenchmark()
    {
        Configure();
        dataSource = new DataSource();
        engine = new AustraEngine(dataSource);
    }

    [Benchmark]
    public Type AustraParseMatrixTrace() =>
        engine.EvalType("let v = vector::nrandom(16) in (v ^ v).trace");

    [Benchmark]
    public Type AustraParseSimpleSum() =>
        engine.EvalType("1.0 + 2.0 * 3.0");

    [Benchmark]
    public double AustraEvalMatrixTrace() =>
        (double)engine.Eval("let v = vector::nrandom(16) in (v ^ v).trace").Value;

    [Benchmark]
    public double AustraEvalSimpleSum() =>
        (double)engine.Eval("1.0 + 2.0 * 3.0").Value;
}
