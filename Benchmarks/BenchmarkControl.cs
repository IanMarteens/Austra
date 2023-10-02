namespace Benchmarks;

public abstract class BenchmarkControl
{
    private const int SIZE = 256;

    protected BenchmarkControl() { }

    public static int Configure()
    {
        return SIZE;
    }
}
