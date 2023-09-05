namespace Ostara;

public sealed class SeriesNode: VarNode
{
    private static readonly string[] freq2str =
    {
        ", 1D", ", 1W", ", 2W", ", 1M", ", 2M", ", 3M", ", 6M", ", 1Y", ""
    };

    private readonly Accumulator acc;

    public SeriesNode(ClassNode parent, string varName, string formula, Series value) :
        base(parent, varName, formula, typeof(Series))
    {
        Name = varName;
        TypeName = Type.Name + "/" + value.Type + freq2str[(int)value.Freq];
        Series = value;
        acc = value.Stats();
    }

    public SeriesNode(ClassNode parent, string varName, Series value) :
    this(parent, varName, varName, value)
    { }

    public Series Series { get; }

    override public void Show() =>
        RootModel.Instance.AppendResult(Name, Series.ToString());

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }

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
