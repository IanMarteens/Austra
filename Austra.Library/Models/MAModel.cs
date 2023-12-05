namespace Austra.Library;

/// <summary>Represents an moving average model MA(q).</summary>
/// <typeparam name="T">The type of the data source.</typeparam>
public abstract class MAModel<T>
{
    /// <summary>
    /// Initializes a moving average model from a source and a number of degrees.
    /// </summary>
    /// <param name="original">The original source.</param>
    /// <param name="degrees">Degrees, not including the independent term.</param>
    protected MAModel(T original, int degrees) =>
        (Original, Degrees, Prediction) = (original, degrees, default!);

    /// <summary>Gets the data source.</summary>
    public T Original { get; }
    /// <summary>Predicted samples.</summary>
    public T Prediction { get; protected set; }

    /// <summary>The order of the moving average model, i.e. the number of degrees.</summary>
    public int Degrees { get; }

    /// <summary>The independent term, or mean, in the MA model.</summary>
    public double Mean { get; protected set; }

    /// <summary>Inferred coefficients of the moving average model.</summary>
    public DVector Coefficients { get; protected set; }
    /// <summary>Residuals calculated by the iteration process.</summary>
    public DVector Residuals { get; protected set; }

    /// <summary>Gets the total sum of squares.</summary>
    public double TotalSumSquares { get; protected set; }
    /// <summary>Gets the residual sum of squares.</summary>
    public double ResidualSumSquares { get; protected set; }
    /// <summary>Explained variance versus total variance.</summary>
    public double R2 { get; protected set; }

    /// <summary>Fills an array with predicted values.</summary>
    /// <param name="oldValues">Original samples.</param>
    /// <param name="residuals">Residuals calculated by the iteration process.</param>
    /// <returns>Predicted samples.</returns>
    protected double[] Predict(DVector oldValues, DVector residuals)
    {
        double[] newValues = new double[oldValues.Length];
        for (int i = 0; i < Degrees; i++)
            newValues[i] = oldValues[i];
        for (int i = Degrees; i < newValues.Length; ++i)
        {
            double tmpAR = Mean;
            for (int j = 0; j < Degrees; ++j)
                tmpAR += Coefficients[j] * residuals[i - j - 1];
            newValues[i] = tmpAR;
        }
        return newValues;
    }

    /// <summary>Gets the string representation of the autoregressive model.</summary>
    /// <returns>Regression coefficients and goodness of fit.</returns>
    public sealed override string ToString() => ToString("G6", null);

    /// <summary>Gets the string representation of the autoregressive model.</summary>
    /// <param name="format">A format specifier.</param>
    /// <param name="provider">Supplies culture-specific formatting information.</param>
    /// <returns>Regression coefficients and goodness of fit.</returns>
    public string ToString(string? format, IFormatProvider? provider) => new StringBuilder(1024)
        .Append("Coefficients: ").AppendLine(Coefficients.ToString(format, provider))
        .Append("μ: ").AppendLine(Mean.ToString(format, provider))
        .Append("R²: ").AppendLine(R2.ToString(format, provider))
        .ToString();
}

/// <summary>Represents an autoregressive model from a series.</summary>
public sealed class MASModel : MAModel<Series>
{
    /// <summary>
    /// Initializes a moving average model from a series and a number of degrees.
    /// </summary>
    /// <param name="original">The original series.</param>
    /// <param name="degrees">Degrees.</param>
    public MASModel(Series original, int degrees) : base(original, degrees)
    {
        DVector reverse = original.Values.Reverse();
        MACalculator calc = new(degrees, reverse);
        DVector coeffs = calc.Run(200, 1e-9);
        Mean = coeffs[0];
        Coefficients = coeffs[1..];
        Residuals = calc.Residuals;
        double[] newValues = Predict(reverse, Residuals);
        Array.Reverse(newValues);
        Prediction = new(
            original.Name + ".MA(" + degrees + ")",
            original.Ticker,
            original.args, newValues, original);
        (TotalSumSquares, ResidualSumSquares, R2) =
            Original.Values.GetSumSquares(Prediction.Values);
    }
}

/// <summary>Represents a moving average model from a vector.</summary>
public sealed class MAVModel : MAModel<DVector>
{
    /// <summary>
    /// Initializes a moving average model from a vector and a number of degrees.
    /// </summary>
    /// <param name="original">The original vector.</param>
    /// <param name="degrees">Degrees.</param>
    public MAVModel(DVector original, int degrees) : base(original, degrees)
    {
        MACalculator calc = new(degrees, original);
        DVector coeffs = calc.Run(128, 1e-9);
        Mean = coeffs[0];
        Coefficients = coeffs[1..];
        Residuals = calc.Residuals;
        Prediction = new(Predict(original, Residuals));
        (TotalSumSquares, ResidualSumSquares, R2) = Original.GetSumSquares(Prediction);
    }
}

