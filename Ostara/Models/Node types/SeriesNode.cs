using System.Windows.Data;

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

    override public void Show() =>
        RootModel.Instance.AppendControl(Formula, Model.ToString(),
            new SeriesViewModel(this).CreateControl());

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

public sealed class SeriesViewModel : Entity
{
    private readonly SeriesNode node;
    private readonly OxyPlot.PlotModel model;
    private readonly StackPanel toolBar;
    private int selectedSeries = 0;
    private int movingPoints = 30;
    private double ewmaLambda = 0.65;

    public SeriesViewModel(SeriesNode node)
    {
        this.node = node;
        model = VarNode.CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
            .CreateLegend()
            .CreateSeries(node.Model, "Original");
        toolBar = new() { Orientation = Orientation.Horizontal };
    }

    public int MovingPoints
    {
        get => movingPoints;
        set
        {
            if (value > 0 && value < node.Count - 2
                && SetField(ref movingPoints, value) && SelectedSeries is 1 or 2)
            {
                if (model.Series.Count > 0)
                    model.Series.RemoveAt(1);
                model.CreateSeries(SelectedSeries == 1
                    ? node.Model.MovingAvg(MovingPoints)
                    : node.Model.MovingStd(MovingPoints),
                    "Reference");
                model.ResetAllAxes();
                model.InvalidatePlot(true);
            }
        }
    }

    public double EwmaLambda
    {
        get => ewmaLambda;
        set
        {
            if (value >= 0 && value <= 1
                && SetField(ref ewmaLambda, value) && SelectedSeries is 3)
            {
                if (model.Series.Count > 0)
                    model.Series.RemoveAt(1);
                model.CreateSeries(node.Model.EWMA(EwmaLambda), "Reference");
                model.ResetAllAxes();
                model.InvalidatePlot(true);
            }
        }
    }

    public int SelectedSeries
    {
        get => selectedSeries;
        set
        {
            if (SetField(ref selectedSeries, value))
            {
                if (model.Series.Count > 1)
                    model.Series.RemoveAt(1);
                Series? newSeries = selectedSeries switch
                {
                    1 => node.Model.MovingAvg(MovingPoints),
                    2 => node.Model.MovingStd(MovingPoints),
                    3 => node.Model.EWMA(EwmaLambda),
                    _ => null,
                };
                toolBar.Children[2].Visibility = toolBar.Children[3].Visibility =
                    selectedSeries is 1 or 2 ? Visibility.Visible : Visibility.Collapsed;
                toolBar.Children[4].Visibility = toolBar.Children[5].Visibility =
                    selectedSeries is 3 ? Visibility.Visible : Visibility.Collapsed;
                if (newSeries != null)
                    model.CreateSeries(newSeries, "Reference");
                model.ResetAllAxes();
                model.InvalidatePlot(true);
            }
        }
    }

    public UIElement CreateControl()
    {
        StackPanel ctrl = new() { Orientation = Orientation.Vertical };
        toolBar.Children.Add(new Label()
        {
            Content = "Reference:",
            VerticalAlignment = VerticalAlignment.Center,
        });
        ComboBox combo = new()
        {
            Width = 200,
            VerticalAlignment = VerticalAlignment.Center,
            ItemsSource = new string[] { "None", "Moving average", "Moving StdDev", "EWMA" },
            SelectedIndex = 0,
        };
        combo.SetBinding(ComboBox.SelectedIndexProperty, new Binding(nameof(SelectedSeries))
        {
            Source = this,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        toolBar.Children.Add(combo);
        toolBar.Children.Add(new Label()
        {
            Content = "Moving points:",
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
        });
        TextBox textBox = new()
        {
            Width = 50,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
            Text = MovingPoints.ToString(),
            TextAlignment = TextAlignment.Right,
        };
        textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(MovingPoints))
        {
            Source = this,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        toolBar.Children.Add(textBox);
        toolBar.Children.Add(new Label()
        {
            Content = "λ:",
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
        });
        textBox = new()
        {
            Width = 50,
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed,
            Text = MovingPoints.ToString(),
            TextAlignment = TextAlignment.Right,
        };
        textBox.SetBinding(TextBox.TextProperty, new Binding(nameof(EwmaLambda))
        {
            Source = this,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            Converter = new DecConverter(),
        });
        toolBar.Children.Add(textBox);
        ctrl.Children.Add(toolBar);
        ctrl.Children.Add(model.CreateView());
        return ctrl;
    }
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