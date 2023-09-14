namespace Ostara;

public sealed class SeriesNode : VarNode<Series>
{
    private static readonly string[] freq2str =
    {
        ", 1D", ", 1W", ", 2W", ", 1M", ", 2M", ", 3M", ", 6M", ", 1Y", ""
    };

    private readonly Accumulator acc;

    public SeriesNode(ClassNode? parent, string varName, string formula, Series value) :
        base(parent, varName, formula, "Series/" + value.Type + freq2str[(int)value.Freq], value)
        => acc = value.Stats();

    public SeriesNode(ClassNode? parent, string varName, Series value) :
        this(parent, varName, varName, value)
    { }

    override public void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
            StringFormat = "dd/MM/yyyy"
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        OxyPlot.Series.LineSeries lineSeries = new()
        {
            TrackerFormatString = "{1}: {2:dd/MM/yyyy}\n{3}: {4:0.####}",
        };
        foreach (Point<Date> p in Model.Points)
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
        var ctrl = new StackPanel()
        {
            Orientation = Orientation.Vertical,
        };
        var toolBar = new StackPanel()
        {
            Orientation = Orientation.Horizontal,
        };
        toolBar.Children.Add(new Label()
        {
            Content = "Compare series with:",
            VerticalAlignment = VerticalAlignment.Center,
        });
        var combo = new ComboBox()
        {
            Width = 200,
            VerticalAlignment = VerticalAlignment.Center,
            ItemsSource = new string[] { "None", "Moving average", "Moving StdDev" },
            SelectedIndex = 0,
        };
        combo.SelectionChanged += (s, e) =>
        {
            if (model.Series.Count > 1)
                model.Series.RemoveAt(1);
            Series newSeries;
            switch (combo.SelectedIndex)
            {
                case 1:
                    newSeries = Model.MovingAvg(30);
                    break;
                case 2:
                    newSeries = Model.MovingStd(30);
                    break;
                default:
                    model.InvalidatePlot(true);
                    return;
            }
            OxyPlot.Series.LineSeries lineSeries = new()
            {
                TrackerFormatString = "{1}: {2:dd/MM/yyyy}\n{3}: {4:0.####}",
            };
            foreach (Point<Date> p in newSeries.Points)
                lineSeries.Points.Add(
                    new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
            model.Series.Add(lineSeries);
            model.InvalidatePlot(true);
        };
        toolBar.Children.Add(combo);
        ctrl.Children.Add(toolBar);
        ctrl.Children.Add(view);
        RootModel.Instance.AppendControl(Formula, Model.ToString(), ctrl);
    }

    public override string Hint => Model.ToString() + Environment.NewLine + Model.Stats().Hint;

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => Stored ? base.ImageSource : "/images/waves.png";

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

public sealed class PercentileNode : VarNode<Series<double>>
{
    public PercentileNode(ClassNode? parent, string varName, string formula, Series<double> value) :
        base(parent, varName, formula, "Percentiles", value)
    { }


    public PercentileNode(ClassNode? parent, string varName, Series<double> value) :
        this(parent, varName, varName, value)
    { }

    override public void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        OxyPlot.Series.LineSeries lineSeries = new()
        {
            TrackerFormatString = "{1}: {2:0.####}\n{3}: {4:0.####}",
        };
        foreach (Point<double> p in Model.Points)
            lineSeries.Points.Add(new(p.Arg, p.Value));
        model.Series.Add(lineSeries);
        OxyPlot.Wpf.PlotView view = new()
        {
            Model = model,
            Width = 900,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
        };
        RootModel.Instance.AppendControl(Formula, Model.ToString(), view);
    }

    public override string Hint => Model.ToString() + Environment.NewLine + Model.Stats().Hint;

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => Stored ? base.ImageSource : "/images/waves.png";

}