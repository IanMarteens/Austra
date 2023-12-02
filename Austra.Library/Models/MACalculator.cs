namespace Austra.Library;

/// <summary>Estimates the coefficients of a moving average model.</summary>
internal sealed class MACalculator
{
    /// <summary>The vector with samples to model.</summary>
    private readonly DVector original;
    private readonly DVector v;
    /// <summary>The order of the model to estimate.</summary>
    private readonly int size;
    private readonly double[] transposeBuffer;
    private readonly double[] values;
    private double[] residuals;
    private double[] newResiduals;
    /// <summary>
    /// The buffer for the coefficients. The first element is the constant term.
    /// </summary>
    private double[] newCoefficients;
    /// <summary>Auxiliary buffer for the coefficients.</summary>
    private double[] coefficients;

    /// <summary>Creates a calculator, allocating buffers for the algorithm.</summary>
    /// <param name="size">The order of the model to estimate.</param>
    /// <param name="original">The vector with samples to model.</param>
    public MACalculator(int size, DVector original)
    {
        (this.original, v, this.size) = (original, original[size..], size);
        transposeBuffer = GC.AllocateUninitializedArray<double>((size + 1) * (size + 1));
        values = GC.AllocateUninitializedArray<double>((size + 1) * v.Length);
        Array.Fill(values, 1.0, 0, v.Length);
        // Start demeaning the original vector for the first iteration.
        residuals = (double[])(original - original.Mean());
        coefficients = new double[size + 1];
        newResiduals = new double[original.Length];
        newCoefficients = new double[size + 1];
    }

    /// <summary>
    /// Runs the algorithm for a given number of iterations or until the accuracy is reached.
    /// </summary>
    /// <param name="maxIterations">Maximum number of iterations</param>
    /// <param name="accuracy">Minimal accuracy for convergence.</param>
    /// <returns>The calculated coefficients. The first element is the constant term.</returns>
    public DVector Run(int maxIterations, double accuracy)
    {
        int c = v.Length;
        ref double orig = ref MM.GetArrayDataReference((double[])original);
        for (int iter = maxIterations; iter >= 0; iter--)
        {
            // Solve ordinary least squares.
            for (int i = 0, offset = c; i < size; i++, offset += c)
                Array.Copy(residuals, size - i - 1, values, offset, c);
            Matrix x = new(size + 1, c, values);
            x.MultiplyTranspose(x, transposeBuffer).Cholesky().Solve(x * v, newCoefficients);
            // Check for convergence.
            if (newCoefficients.Distance(coefficients) <= accuracy)
                return newCoefficients;
            // Calculate new residuals and switch buffers.
            ref double rc = ref MM.GetArrayDataReference(newCoefficients);
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
            (newCoefficients, coefficients) = (coefficients, newCoefficients);
        }
        return newCoefficients;
    }
}
