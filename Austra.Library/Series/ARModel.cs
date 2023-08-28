namespace Austra.Library;

/// <summary>Represents an autoregressive model.</summary>
public abstract class ARModelBase<T>
{
    /// <summary>The Yule-Walker matrix.</summary>
    protected Matrix matrix;
    /// <summary>Autocorrelation of the source.</summary>
    protected Vector correlations;

    /// <summary>
    /// Initializes an autoregressive model from a source and a number of degrees.
    /// </summary>
    /// <param name="original">The original source.</param>
    /// <param name="degrees">Degrees.</param>
    protected ARModelBase(T original, int degrees) =>
        (Original, Degrees, Prediction) = (original, degrees, default!);

    /// <summary>Gets the data source.</summary>
    public T Original { get; }
    /// <summary>Predicted samples.</summary>
    public T Prediction { get; protected set; }

    /// <summary>The order of the autoregressive model, i.e. the number of degrees.</summary>
    public int Degrees { get; }

    /// <summary>Inferred coefficients of the autoregressive model.</summary>
    public Vector Coefficients { get; protected set; }

    /// <summary>The correlation matrix of the autoregressive model.</summary>
    public Matrix Matrix => matrix;
    /// <summary>Gets the correlations.</summary>
    public Vector Correlations => correlations;

    /// <summary>Gets the total sum of squares.</summary>
    public double TotalSumSquares { get; protected set; }
    /// <summary>Gets the residual sum of squares.</summary>
    public double ResidualSumSquares { get; protected set; }
    /// <summary>Explained variance versus total variance.</summary>
    public double R2 { get; protected set; }

    /// <summary>Fills an array with predicted values.</summary>
    /// <param name="oldValues">Original samples.</param>
    /// <returns>Predicted samples.</returns>
    protected double[] Predict(double[] oldValues)
    {
        double[] newValues = new double[oldValues.Length];
        for (int i = 0; i < Degrees; i++)
            newValues[i] = oldValues[i];
        for (int i = Degrees; i < newValues.Length; ++i)
        {
            double tmpAR = 0.0;
            for (int j = 0; j < Degrees; ++j)
                tmpAR += Coefficients[j] * oldValues[i - j - 1];
            newValues[i] = tmpAR;
        }
        return newValues;
    }

    /// <summary>Gets the string representation of the autoregressive model.</summary>
    public sealed override string ToString() => new StringBuilder(1024)
        .Append("Coefficients: ").Append(Coefficients).AppendLine()
        .Append("(R² = ").Append(R2.ToString("G6")).Append(')').AppendLine()
        .ToString();
}

/// <summary>Represents an autoregressive model from a series.</summary>
public sealed class ARSModel : ARModelBase<Series>
{
    /// <summary>
    /// Initializes an autoregressive model from a series and a number of degrees.
    /// </summary>
    /// <param name="original">The original series.</param>
    /// <param name="degrees">Degrees.</param>
    public ARSModel(Series original, int degrees) : base(original, degrees)
    {
        Coefficients = original.AutoRegression(degrees, out matrix, out correlations);
        double[] newValues = Predict((double[])original.GetValues().Reverse());
        Array.Reverse(newValues);
        Prediction = new(
            original.Name + ".AR(" + degrees + ")",
            original.Ticker,
            original.args, newValues, original);
        (TotalSumSquares, ResidualSumSquares, R2) =
            Original.GetValues().GetSumSquares(Prediction.GetValues());
    }
}

/// <summary>Represents an autoregressive model from a vector.</summary>
public sealed class ARVModel: ARModelBase<Vector>
{
    /// <summary>
    /// Initializes an autoregressive model from a vector and a number of degrees.
    /// </summary>
    /// <param name="original">The original vector.</param>
    /// <param name="degrees">Degrees.</param>
    public ARVModel(Vector original, int degrees) : base(original, degrees)
    {
        Coefficients = original.AutoRegression(degrees, out matrix, out correlations);
        Prediction = new(Predict((double[])original));
        (TotalSumSquares, ResidualSumSquares, R2) = Original.GetSumSquares(Prediction);
    }
}
