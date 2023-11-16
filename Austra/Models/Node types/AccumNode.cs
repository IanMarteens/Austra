namespace Austra;

/// <summary>A view-model for a running statistics node.</summary>
public sealed class AccumNode : VarNode<Accumulator>
{
    public AccumNode(string formula, Accumulator value) :
        base(formula, value)
    { }

    public AccumNode(ClassNode? parent, string varName, Accumulator value) :
        base(parent, varName, value)
    { }

    public override string TypeName => "Statistics";

    public override Visibility ImageVisibility => Visibility.Visible;

    public override void Show() => RootModel.Instance.AppendResult(Formula, Model.ToString());

    [Category("Stats")]
    public long Count => Model.Count;

    [Category("Stats")]
    public double Min => Model.Minimum;

    [Category("Stats")]
    public double Max => Model.Maximum;

    [Category("Stats")]
    public double Mean => Model.Mean;

    [Category("Stats")]
    public double Variance => Model.Variance;

    [Category("Stats")]
    public double Volatility => Model.StandardDeviation;

    [Category("Stats")]
    public double Skewness => Model.Skewness;

    [Category("Stats")]
    public double Kurtosis => Model.Kurtosis;
}