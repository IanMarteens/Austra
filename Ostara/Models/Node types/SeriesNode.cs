namespace Ostara;

public sealed class SeriesNode: VarNode
{
    private static readonly string[] freq2str =
    {
        ", 1D", ", 1W", ", 2W", ", 1M", ", 2M", ", 3M", ", 6M", ", 1Y", ""
    };

    private readonly Accumulator acc;

    public SeriesNode(ClassNode? parent, string varName, string formula, Series value) :
        base(parent, varName, formula, typeof(Series))
    {
        Name = varName;
        TypeName = Type.Name + "/" + value.Type + freq2str[(int)value.Freq];
        Series = value;
        acc = value.Stats();
    }

    public SeriesNode(ClassNode? parent, string varName, Series value) :
    this(parent, varName, varName, value)
    { }

    public Series Series { get; }

    override public void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        OxyPlot.Series.LineSeries lineSeries = new();
        foreach (Point<Date> p in Series.Points)
            lineSeries.Points.Add(
                new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
        model.Series.Add(lineSeries);
        OxyPlot.Wpf.PlotView view = new()
        {
            Model = model,
            Width = 900,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
        };
        RootModel.Instance.AppendControl(Name, Series.ToString(), view);
    }

    public override string Hint => Series.Stats().Hint;

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
