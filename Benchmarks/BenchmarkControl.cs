namespace Benchmarks;

public abstract class BenchmarkControl
{
    private const int SIZE = 56;

    protected BenchmarkControl() { }

    public static int Configure() => SIZE;
}
