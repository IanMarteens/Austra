using OxyPlot;
using System.Windows.Data;
using System.Windows.Documents;

namespace Ostara;

public sealed class MvoNode : VarNode<MvoModel>
{
    public MvoNode(ClassNode? parent, string varName, string formula, MvoModel value) :
        base(parent, varName, formula, "MVO Model", value)
    {
        // Create the frontier.
        Frontier = new("Frontier", null,
            Model.Portfolios.Select(p => p.StdDev).ToArray(),
            Model.Portfolios.Select(p => p.Mean).ToArray(),
            SeriesType.Raw);
    }

    public MvoNode(ClassNode? parent, string varName, MvoModel value) :
        this(parent, varName, varName, value)
    { }

    public Series<double> Frontier { get; }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/mvo.png";

    public override void Show() =>
        RootModel.Instance.AppendResult(
            Formula, CreateTable(),
            new MvoViewModel(this).CreateControl());

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

public sealed class MvoViewModel : Entity
{
    private readonly PlotModel fModel;
    private readonly PlotModel wModel;
    private readonly OxyPlot.Series.PieSeries pieSeries;
    private readonly List<Tuple<string, double>> weights;
    private double ret;
    private double variance;
    private double stddev;
    //private double riskFreeReturn;

    public MvoViewModel(MvoNode node)
    {
        Node = node;
        // Initialize targets.
        Portfolio p = node.Model[^1];
        (ret, variance, stddev) = (MinRet, MinVar, MinStd) = (p.Mean, p.Variance, p.StdDev);
        p = node.Model[0];
        (MaxRet, MaxVar, MaxStd) = (p.Mean, p.Variance, p.StdDev);
        // Initialize weights.
        RVector w = node.Model[^1].Weights;
        weights = new(w.Length);
        for (int i = 0; i < w.Length; i++)
            weights.Add(new(node.Model.Labels[i], w[i]));
        // Create the OxyPlot model for the pie chart.
        wModel = new() { Title = "Weights" };
        pieSeries = new();
        foreach (Tuple<string, double> t in weights)
            pieSeries.Slices.Add(new(t.Item1, t.Item2));
        wModel.Series.Add(pieSeries);
        // The efficient frontier model.
        fModel = VarNode.CreateOxyModel(
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
            .CreateSeries(node.Frontier);
    }

    public MvoNode Node { get; }

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
                Node.Model.Portfolios,
                Node.Model.Covariance, value);
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
                Node.Model.Portfolios,
                Node.Model.Covariance, (double)value);
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
                Node.Model.Portfolios,
                Node.Model.Covariance, varn);
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
            var w = this.weights[i] = new(Node.Model.Labels[i], weights[i]);
            pieSeries?.Slices.Add(new(w.Item1, w.Item2));
        }
        wModel?.InvalidatePlot(true);
    }

    public UIElement CreateControl()
    {
        Grid grid = new()
        {
            Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromArgb(0xFF, 0x20, 0x20, 0x20)),
            Margin = new Thickness(0, 0, 2, 0),
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
        grid.Children.Add(CreateTextBlock("Ret", 0, 0));
        grid.Children.Add(CreateTextBlock("σ", 0, 1));
        grid.Children.Add(CreateTextBlock("σ²", 0, 2));
        grid.Children.Add(CreateSlider(MinRet, MaxRet, Ret, nameof(Ret), 1, 0));
        grid.Children.Add(CreateSlider(MinStd, MaxStd, StdDev, nameof(StdDev), 1, 1));
        grid.Children.Add(CreateSlider(MinVar, MaxVar, Variance, nameof(Variance), 1, 2));
        StackPanel panel = new() { Orientation = Orientation.Horizontal };
        panel.Children.Add(grid);
        panel.Children.Add(new Separator() { Width = 2 });
        panel.Children.Add(fModel.CreateView(300));
        panel.Children.Add(new Separator() { Width = 2 });
        panel.Children.Add(wModel.CreateView(250));
        return panel;

        Slider CreateSlider(
            double min, double max, double value,
            string property, int row, int column)
        {
            Slider slider = new()
            {
                Minimum = min,
                Maximum = max,
                Value = value,
                Orientation = Orientation.Vertical,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Stretch,
                Margin = new Thickness(4, 0, 4, 1),
            };
            slider.SetBinding(Slider.ValueProperty, new Binding(property)
            {
                Source = this,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            });
            Grid.SetRow(slider, row);
            Grid.SetColumn(slider, column);
            return slider;
        }

        static TextBlock CreateTextBlock(string caption, int row, int column)
        {
            TextBlock result = new()
            {
                Text = caption,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White),
                Padding = new Thickness(4, 0, 4, 0),
            };
            Grid.SetRow(result, row);
            Grid.SetColumn(result, column);
            return result;
        }
    }
}

