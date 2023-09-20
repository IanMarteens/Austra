using OxyPlot;
using OxyPlot.Annotations;
using System.Windows.Data;
using System.Windows.Documents;

namespace Ostara;

public sealed class MvoNode : VarNode<MvoModel>
{
    private OxyPlot.Series.PieSeries? pieSeries;
    private PlotModel? fModel;
    private PlotModel? wModel;
    private double ret;
    private double variance;
    private double stddev;
    //private double riskFreeReturn;

    public MvoNode(ClassNode? parent, string varName, string formula, MvoModel value) :
        base(parent, varName, formula, "MVO Model", value)
    {
        // Create the frontier.
        Frontier = new("Frontier", null,
            Model.Portfolios.Select(p => p.StdDev).ToArray(),
            Model.Portfolios.Select(p => p.Mean).ToArray(),
            SeriesType.Raw);
        // Initialize targets.
        Portfolio p = Model[^1];
        (ret, variance, stddev) = (MinRet, MinVar, MinStd) = (p.Mean, p.Variance, p.StdDev);
        p = Model[0];
        (MaxRet, MaxVar, MaxStd) = (p.Mean, p.Variance, p.StdDev);
        // Initialize weights.
        RVector w = Model[^1].Weights;
        Weights = new();
        for (int i = 0; i < w.Length; i++)
            Weights.Add(new(Model.Labels[i], w[i]));
    }

    public MvoNode(ClassNode? parent, string varName, MvoModel value) :
        this(parent, varName, varName, value)
    { }

    public double MinRet { get; }
    public double MinVar { get; }
    public double MinStd { get; }
    public double MaxRet { get; }
    public double MaxVar { get; }
    public double MaxStd { get; }

    public double Ret
    {
        get => ret;
        set
        {
            if (value < MinRet || value > MaxRet)
                return;
            InterpolatedPortfolio? ip = Optimizer.GetTargetReturnEfficientPortfolio(
                Model.Portfolios,
                Model.Covariance, value);
            if (ip == null)
                return;
            SetFields(value, ip.StdDev, ip.Variance);
            SetWeights(ip.Weights);
        }
    }

    public double Variance
    {
        get => variance;
        set
        {
            if (value < MinVar || value > MaxVar)
                return;
            InterpolatedPortfolio? ip = Optimizer.GetTargetVolatilityEfficientPortfolio(
                Model.Portfolios,
                Model.Covariance, (double)value);
            if (ip == null)
                return;
            SetFields(ip.Mean, Math.Sqrt((double)value), (double)value);
            SetWeights(ip.Weights);
        }
    }

    public double StdDev
    {
        get => stddev;
        set
        {
            if (value < MinStd || value > MaxStd)
                return;
            double varn = value * value;
            InterpolatedPortfolio? ip = Optimizer.GetTargetVolatilityEfficientPortfolio(
                Model.Portfolios,
                Model.Covariance, varn);
            if (ip == null)
                return;
            SetFields(ip.Mean, value, varn);
            SetWeights(ip.Weights);
        }
    }

    private void SetFields(double mean, double std, double varn)
    {
        SetField(ref ret, Clip(mean, MinRet, MaxRet), nameof(Ret));
        SetField(ref variance, Clip(varn, MinVar, MaxVar), nameof(Variance));
        SetField(ref stddev, Clip(std, MinStd, MaxStd), nameof(StdDev));
        fModel?.UpdateLine(stddev);

        static double Clip(double value, double min, double max)
        {
            if (value <= min)
                return min;
            if (value >= max)
                return max;
            return value;
        }
    }

    private void SetWeights(RVector weights)
    {
        pieSeries?.Slices.Clear();
        for (int i = 0; i < weights.Length; i++)
        {
            var w = Weights[i] = new(Model.Labels[i], weights[i]);
            pieSeries?.Slices.Add(new(w.Item1, w.Item2));
        }
        wModel?.InvalidatePlot(true);
    }

    public ObservableCollection<Tuple<string, double>> Weights { get; } = new();

    public Series<double> Frontier { get; }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/mvo.png";

    public override void Show()
    {
        if (wModel is null)
        {
            wModel = new() { Title = "Weights" };
            pieSeries = new();
            foreach (Tuple<string, double> w in Weights)
                pieSeries.Slices.Add(new(w.Item1, w.Item2));
            wModel.Series.Add(pieSeries);
        }
        fModel ??= CreateOxyModel(
            new OxyPlot.Axes.LinearAxis()
            {
                Title = "σ",
                TitleFontWeight = 500d,
            },
            new OxyPlot.Axes.LinearAxis()
            {
                Title = "Return",
                TitleFontWeight = 500d,
            })
            .CreateLine(StdDev)
            .CreateSeries(Frontier);
        Slider retSlider = new()
        {
            Minimum = MinRet,
            Maximum = MaxRet,
            Value = Ret,
            Orientation = Orientation.Vertical,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
            Margin = new Thickness(4, 0, 4, 1),
        };
        retSlider.SetBinding(Slider.ValueProperty, new Binding(nameof(Ret))
        {
            Source = this,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        Slider stdSlider = new()
        {
            Minimum = MinStd,
            Maximum = MaxStd,
            Value = StdDev,
            Orientation = Orientation.Vertical,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
            Margin = new Thickness(4, 0, 4, 1),
        };
        stdSlider.SetBinding(Slider.ValueProperty, new Binding(nameof(StdDev))
        {
            Source = this,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        Slider varSlider = new()
        {
            Minimum = MinVar,
            Maximum = MaxVar,
            Value = Variance,
            Orientation = Orientation.Vertical,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
            Margin = new Thickness(4, 0, 4, 1),
        };
        varSlider.SetBinding(Slider.ValueProperty, new Binding(nameof(Variance))
        {
            Source = this,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        TextBlock retLabel = new()
        {
            Text = "Ret",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
            Padding = new Thickness(4, 0, 4, 0),
        };
        TextBlock stdLabel = new()
        {
            Text = "σ",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
            Padding = new Thickness(4, 0, 4, 0),
        };
        TextBlock varLabel = new()
        {
            Text = "σ²",
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
            Padding = new Thickness(4, 0, 4, 0),
        };
        var grid = new Grid()
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0xFF, 0x20, 0x20, 0x20)),
            Margin = new Thickness(0, 0, 2, 0),
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(retLabel, 0);
        Grid.SetRow(retLabel, 0);
        Grid.SetColumn(retSlider, 0);
        Grid.SetRow(retSlider, 1);
        Grid.SetColumn(stdLabel, 1);
        Grid.SetRow(stdLabel, 0);
        Grid.SetColumn(stdSlider, 1);
        Grid.SetRow(stdSlider, 1);
        Grid.SetColumn(varLabel, 2);
        Grid.SetRow(stdLabel, 0);
        Grid.SetColumn(varSlider, 2);
        Grid.SetRow(varSlider, 1);
        grid.Children.Add(retLabel);
        grid.Children.Add(stdLabel);
        grid.Children.Add(varLabel);
        grid.Children.Add(retSlider);
        grid.Children.Add(stdSlider);
        grid.Children.Add(varSlider);
        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,
        };
        panel.Children.Add(grid);
        panel.Children.Add(new Separator() { Width = 2 });
        panel.Children.Add(fModel.CreateView(300));
        panel.Children.Add(new Separator() { Width = 2 });
        panel.Children.Add(wModel.CreateView(250));
        RootModel.Instance.AppendResult(Formula, CreateTable(), panel);
    }

    private Table CreateTable()
    {
        Table result = new();
        for (int i = 0; i < 3 + Model.Size; i++)
            result.Columns.Add(new TableColumn() { Width = new GridLength(100, GridUnitType.Pixel) });
        TableRowGroup group = new();
        TableRow row = new();
        row.Cells.Add(NewHdr("λ"));
        row.Cells.Add(NewHdr("Return"));
        row.Cells.Add(NewHdr("Volatility"));
        foreach (string name in Model.Labels)
            row.Cells.Add(NewHdr(name));
        group.Rows.Add(row);
        for (int i = 0; i < Model.Length; i++)
        {
            Portfolio p = Model[i];
            row = new();
            row.Cells.Add(NewCell(p.Lambda.ToString("F2")));
            row.Cells.Add(NewCell(p.Mean.ToString("G6")));
            row.Cells.Add(NewCell(p.StdDev.ToString("G6")));
            for (int j = 0; j < Model.Size; j++)
                row.Cells.Add(NewCell(p.Weights[j].ToString("F6")));
            group.Rows.Add(row);
        }
        result.RowGroups.Add(group);
        return result;

        static TableCell NewCell(string text) => new(new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Right
        });

        static TableCell NewHdr(string header) => new(new Paragraph(new Run(header))
        {
            FontWeight = System.Windows.FontWeights.DemiBold,
            TextAlignment = TextAlignment.Right
        });
    }
}

