namespace Benchmarks;

public class ParserBenchmark : BenchmarkControl
{
    private readonly IDataSource dataSource;
    private readonly AustraEngine engine;

    public ParserBenchmark()
    {
        Configure();
        dataSource = new DataSource();
        engine = new AustraEngine(dataSource);
    }

    [Benchmark]
    public Type[] AustraParseMatrixTrace() =>
        engine.EvalType("let v = vector::nrandom(16) in (v ^ v).trace");

    [Benchmark]
    public Type[] AustraParseCholeskyCheck() =>
        engine.EvalType("let m0 = matrix::random(10) + 0.01, sm = m0*m0', c = sm.chol in sm - c*c'");

    [Benchmark]
    public Type[] AustraParseSimpleSum() =>
        engine.EvalType("1.0 + 2.0 * 3.0");

    [Benchmark]
    public Type[] AustraParseFunctionCall() =>
        engine.EvalType("sin(1) + cos(2)");

    [Benchmark]
    public void AustraEvalMatrixTrace() =>
        engine.Eval("let v = vector::nrandom(16) in (v ^ v).trace");

    [Benchmark]
    public void AustraEvalSimpleSum() =>
        engine.Eval("1.0 + 2.0 * 3.0");

    [Benchmark]
    public void AustraEvalFunctionCall() =>
        engine.Eval("sin(1) + cos(2)");
}
