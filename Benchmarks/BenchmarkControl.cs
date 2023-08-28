using MathNet.Numerics.Providers.FourierTransform;

namespace Benchmarks;

public abstract class BenchmarkControl
{
    private const int SIZE = 256;
    private static readonly bool USE_MANAGED = true;

    protected BenchmarkControl() { }

    public static int Configure()
    {
        if (USE_MANAGED)
        {
            Control.UseManaged();
            FourierTransformControl.UseManaged();
        }
        else
        {
            Control.UseNativeMKL();
            FourierTransformControl.UseNativeMKL();
        }
        return SIZE;
    }
}
