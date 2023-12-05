namespace Austra.Library;

/// <summary>Estimates the coefficients of a moving average model.</summary>
/// <param name="size">The order of the model to estimate.</param>
/// <param name="original">The vector with samples to model.</param>
internal sealed class MACalculator(int size, DVector original)
{
    /// <summary>The order of the model to estimate.</summary>
    private readonly int size = size;
    /// <summary>The vector with samples to model.</summary>
    private readonly DVector original = original;
    /// <summary>The original vector without the first <see cref="size"/> elements.</summary>
    private readonly DVector v = original[size..];
    /// <summary>Residuals after the last iteration.</summary>
    /// <remarks>Its initial value is the original vector demeaned.</remarks>
    private double[] residuals = (double[])(original - original.Mean());

    /// <summary>Gets the residuals after the last iteration.</summary>
    public DVector Residuals => residuals;

    /// <summary>
    /// Runs the algorithm for a given number of iterations or until the accuracy is reached.
    /// </summary>
    /// <param name="maxIterations">Maximum number of iterations</param>
    /// <param name="accuracy">Minimal accuracy for convergence.</param>
    /// <returns>The calculated coefficients. The first element is the constant term.</returns>
    public DVector Run(int maxIterations, double accuracy)
    {
        double[] values = GC.AllocateUninitializedArray<double>((size + 1) * v.Length);
        Array.Fill(values, 1.0, 0, v.Length);
        double[] transposeBuffer = GC.AllocateUninitializedArray<double>((size + 1) * (size + 1));
        double[] transformBuffer = GC.AllocateUninitializedArray<double>(size + 1);
        // Buffers for the coefficients.
        double[] coeffs = new double[size + 1];
        double[] newCoeffs = new double[size + 1];
        // A buffer for the residuals.
        double[] newResiduals = new double[original.Length];
        int c = v.Length;
        ref double orig = ref MM.GetArrayDataReference((double[])original);
        for (int iter = maxIterations; iter >= 0; iter--)
        {
            // Solve ordinary least squares.
            for (int i = 0, offset = c; i < size; i++, offset += c)
                Array.Copy(residuals, size - i - 1, values, offset, c);
            Matrix x = new(size + 1, c, values);
            x.MultiplyTranspose(x, transposeBuffer)
                .Cholesky()
                .Solve(x.Transform(v, transformBuffer), newCoeffs);
            // Check for convergence.
            if (newCoeffs.Distance(coeffs) <= accuracy)
                return newCoeffs;
            // Calculate new residuals and switch buffers.
            ref double rc = ref MM.GetArrayDataReference(newCoeffs);
            ref double r = ref MM.GetArrayDataReference(residuals);
            ref double nr = ref MM.GetArrayDataReference(newResiduals);
            for (int i = 0; i < size; i++)
            {
                double d = Add(ref orig, i) - rc;
                for (int j = 1; j <= Min(i, size); j++)
                    d -= Add(ref rc, j) * Add(ref r, i - j);
                Add(ref nr, i) = d;
            }
            for (int i = size; i < newResiduals.Length; i++)
            {
                double d = Add(ref orig, i) - rc;
                for (int j = 1; j <= size; j++)
                    d -= Add(ref rc, j) * Add(ref r, i - j);
                Add(ref nr, i) = d;
            }
            // Swap buffers for the next iteration.
            (residuals, newResiduals) = (newResiduals, residuals);
            (newCoeffs, coeffs) = (coeffs, newCoeffs);
        }
        return newCoeffs;
    }
}
