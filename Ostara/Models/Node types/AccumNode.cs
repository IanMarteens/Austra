namespace Ostara;

/// <summary>A view-model for a running statistics node.</summary>
public sealed class AccumNode : VarNode
{
    private readonly Accumulator acc;

    public AccumNode(ClassNode? parent, string varName, string formula, Accumulator value) :
        base(parent, varName, formula, "Statistics")
    {
        acc = value;
    }

    public AccumNode(ClassNode? parent, string varName, Accumulator value) :
        this(parent, varName, varName, value)
    { }

    public override void Show() => RootModel.Instance.AppendResult(Name, acc.ToString());

    [Category("Stats")]
    public long Count => acc.Count;

    [Category("Stats")]
    public double Min => acc.Minimum;

    [Category("Stats")]
    public double Max => acc.Maximum;

    [Category("Stats")]
    public double Mean => acc.Mean;

    [Category("Stats")]
    public double Variance => acc.Variance;

    [Category("Stats")]
    public double Volatility => acc.StandardDeviation;

    [Category("Stats")]
    public double Skewness => acc.Skewness;

    [Category("Stats")]
    public double Kurtosis => acc.Kurtosis;
}