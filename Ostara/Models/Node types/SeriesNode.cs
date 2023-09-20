namespace Ostara;

public sealed class SeriesNode : VarNode<Series>
{
    private static readonly string[] freq2str =
    {
        ", 1D", ", 1W", ", 2W", ", 1M", ", 2M", ", 3M", ", 6M", ", 1Y", ""
    };

    public SeriesNode(ClassNode? parent, string varName, string formula, Series value) :
        base(parent, varName, formula, "Series/" + value.Type + freq2str[(int)value.Freq], value)
    { }

    public SeriesNode(ClassNode? parent, string varName, Series value) :
        this(parent, varName, varName, value)
    { }

    override public void Show()
    {
        OxyPlot.PlotModel model = CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
            .CreateSeries(Model);
        StackPanel ctrl = new() { Orientation = Orientation.Vertical };
        StackPanel toolBar = new() { Orientation = Orientation.Horizontal };
        toolBar.Children.Add(new Label()
        {
            Content = "Compare series with:",
            VerticalAlignment = VerticalAlignment.Center,
        });
        ComboBox combo = new()
        {
            Width = 200,
            VerticalAlignment = VerticalAlignment.Center,
            ItemsSource = new string[] { "None", "Moving average", "Moving StdDev", "EWMA" },
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
                case 3:
                    newSeries = Model.EWMA(0.65); 
                    break;
                default:
                    model.InvalidatePlot(true);
                    return;
            }
            model.CreateSeries(newSeries);
            model.ResetAllAxes();
            model.InvalidatePlot(true);
        };
        toolBar.Children.Add(combo);
        ctrl.Children.Add(toolBar);
        ctrl.Children.Add(model.CreateView());
        RootModel.Instance.AppendControl(Formula, Model.ToString(), ctrl);
    }

    public override string Hint => Model.ToString() + Environment.NewLine + Model.Stats.Hint;

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => Stored ? base.ImageSource : "/images/waves.png";

    [Category("Stats")]
    public long Count => Model.Stats.Count;

    [Category("Stats")]
    public double Min => Model.Stats.Minimum;

    [Category("Stats")]
    public double Max => Model.Stats.Maximum;

    [Category("Stats")]
    public double Mean => Model.Stats.Mean;

    [Category("Stats")]
    public double Variance => Model.Stats.Variance;

    [Category("Stats")]
    public double Volatility => Model.Stats.StandardDeviation;

    [Category("Stats")]
    public double Skewness => Model.Stats.Skewness;

    [Category("Stats")]
    public double Kurtosis => Model.Stats.Kurtosis;
}

public sealed class PercentileNode : VarNode<Series<double>>
{
    public PercentileNode(ClassNode? parent, string varName, string formula, Series<double> value) :
        base(parent, varName, formula, "Percentiles", value)
    { }


    public PercentileNode(ClassNode? parent, string varName, Series<double> value) :
        this(parent, varName, varName, value)
    { }

    override public void Show() =>
        RootModel.Instance.AppendControl(Formula, Model.ToString(),
            CreateOxyModel().CreateSeries(Model).CreateView());

    public override string Hint => Model.ToString() + Environment.NewLine + Model.Stats.Hint;

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => Stored ? base.ImageSource : "/images/waves.png";
}

public sealed class CorrelogramNode : VarNode<Series<int>>
{
    public CorrelogramNode(ClassNode? parent, string varName, string formula, Series<int> value) :
        base(parent, varName, formula, "Percentiles", value)
    { }


    public CorrelogramNode(ClassNode? parent, string varName, Series<int> value) :
        this(parent, varName, varName, value)
    { }

    override public void Show() =>
        RootModel.Instance.AppendControl(Formula, Model.ToString(),
            CreateOxyModel().CreateStepSeries(Model.GetValues()).CreateView());

    public override string Hint => Model.ToString() + Environment.NewLine + Model.Stats.Hint;

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => Stored ? base.ImageSource : "/images/waves.png";

}