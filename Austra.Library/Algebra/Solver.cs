namespace Austra.Library;

/// <summary>A simple solver for a function f(x) = 0.</summary>
public static class Solver
{
    /// <summary>Performs a Newton-Raphson iteration to find a root of f(x) = 0.</summary>
    /// <param name="f">The function to find its root.</param>
    /// <param name="df">The derivative of the function.</param>
    /// <param name="initialValue">The initial estimate for iteration.</param>
    /// <param name="accuracy">The desired accuracy.</param>
    /// <param name="maxIterations">Maximum number of iterations allowed.</param>
    /// <returns>The approximated root.</returns>
    public static double Solve(
        Func<double, double> f,
        Func<double, double> df,
        double initialValue,
        double accuracy,
        int maxIterations)
    {
        bool alreadyNudged = false;
        double x = initialValue, fx = 0;
        while (maxIterations-- > 0)
        {
            fx = f(x);
            if (Abs(fx) < accuracy)
                return x;
            double dfx = df(x);
            if (Abs(dfx) < accuracy)
            {
                if (alreadyNudged)
                    throw new ConvergenceException();
                alreadyNudged = true;
                double f1 = f(x + 1), f2 = f(x - 1);
                x = Abs(f1) < Abs(f2) ? x + 1 : x - 1;
                continue;
            }
            x -= fx / dfx;
        }
        if (Abs(fx) > accuracy)
            throw new ConvergenceException();
        return x;
    }

    /// <summary>Performs a Newton-Raphson iteration to find a root of f(x) = 0.</summary>
    /// <param name="f">The function to find its root.</param>
    /// <param name="df">The derivative of the function.</param>
    /// <param name="initialValue">The initial estimate for iteration.</param>
    /// <param name="accuracy">The desired accuracy.</param>
    /// <returns>The approximated root.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Solve(
        Func<double, double> f,
        Func<double, double> df,
        double initialValue,
        double accuracy = 1e-9) => Solve(f, df, initialValue, accuracy, 100);

    /// <summary>Performs a Newton-Raphson iteration to find a root of f(x) = 0.</summary>
    /// <param name="f">The function to find its root.</param>
    /// <param name="df">The derivative of the function.</param>
    /// <param name="initialValue">The initial estimate for iteration.</param>
    /// <returns>The approximated root.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double Solve(
        Func<double, double> f,
        Func<double, double> df,
        double initialValue) => Solve(f, df, initialValue, 1e-9, 100);
}