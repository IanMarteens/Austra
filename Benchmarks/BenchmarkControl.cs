namespace Benchmarks;

public abstract class BenchmarkControl
{
    private const int SIZE = 32;

    protected BenchmarkControl() { }

    public static int Configure()
    {
        return SIZE;
    }
}
