namespace Austra;

/// <summary>Base class for comparing series and vectors.</summary>
/// <typeparam name="T">The type of the compared items.</typeparam>
public abstract class CompareNodeBase<T> : VarNode<Plot<T>> where T : IFormattable
{
    protected CompareNodeBase(string formula, Plot<T> model)
        : base(formula, model)
    { }

    protected CompareNodeBase(ClassNode? parent, string name, Plot<T> model)
        : base(parent, name, model)
    { }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/compare.png";
}

/// <summary>Compares two time series.</summary>
public sealed class CompareNode : CompareNodeBase<Series>
{
    public CompareNode(string formula, Plot<Series> value) :
        base(formula, value)
    { }

    public CompareNode(ClassNode? parent, string varName, Plot<Series> value) :
        base(parent, varName, value)
    { }

    public override string TypeName => "Series plot";

    public override void Show()
    {
        if (Model.HasSecond)
            RootModel.Instance.AppendControl(Formula,
                Model.First.ToString() + " vs " + Model.Second!.ToString(),
                CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
                    .CreateLegend()
                    .CreateSeries(Model.First, "First")
                    .CreateSeries(Model.Second!, "Second")
                    .CreateView());
        else
            RootModel.Instance.AppendControl(Formula,
                Model.First.ToString(),
                CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
                    .CreateLegend()
                    .CreateSeries(Model.First)
                    .CreateView());
    }
}

public sealed class CompareVNode : CompareNodeBase<RVector>
{
    public CompareVNode(string formula, Plot<RVector> value) :
        base(formula, value)
    { }

    public CompareVNode(ClassNode? parent, string varName, Plot<RVector> value) :
        base(parent, varName, value)
    { }

    public override string TypeName => "Vector plot";

    public override void Show()
    {
        if (Model.HasSecond)
            RootModel.Instance.AppendControl(Formula,
                $"ℝ({Model.First.Length}) vs ℝ({Model.Second!.Length})",
                CreateOxyModel(new OxyPlot.Axes.LinearAxis())
                    .CreateLegend()
                    .CreateStepSeries(Model.First, "First")
                    .CreateStepSeries(Model.Second!, "Second")
                    .CreateView());
        else
            RootModel.Instance.AppendControl(Formula,
                $"ℝ({Model.First.Length})",
                CreateOxyModel(new OxyPlot.Axes.LinearAxis())
                    .CreateLegend()
                    .CreateStepSeries(Model.First)
                    .CreateView());
    }
}

public sealed class CompareCVNode : CompareNodeBase<CVector>
{
    public CompareCVNode(string formula, Plot<CVector> value) :
        base(formula, value)
    { }

    public CompareCVNode(ClassNode? parent, string varName, Plot<CVector> value) :
        base(parent, varName, value)
    { }

    public override string TypeName => "Complex vector plot";

    public override void Show() =>
        RootModel.Instance.AppendControl(Formula,
            Model.HasSecond
                ? $"ℂ({Model.First.Length}) vs ℂ({Model.Second!.Length})"
                : $"ℂ({Model.First.Length})",
            CreateControl());

    private StackPanel CreateControl()
    {
        OxyPlot.PlotModel oxyModel = CreateOxyModel(new OxyPlot.Axes.LinearAxis());
        StackPanel panel = new() { Orientation = Orientation.Vertical };
        StackPanel toolBar = new() { Orientation = Orientation.Horizontal };
        toolBar.Children.Add(new Label()
        {
            Content = Model.HasSecond ? "Compare:" : "Plot:",
            VerticalAlignment = VerticalAlignment.Center,
        });
        List<string> references = ["Magnitude", "Phase", "Real", "Imaginary"];
        ComboBox combo = new()
        {
            Width = 200,
            VerticalAlignment = VerticalAlignment.Center,
            ItemsSource = references,
            SelectedIndex = 0,
        };
        combo.SelectionChanged += (s, e) =>
        {
            oxyModel.Series.Clear();
            switch (combo.SelectedIndex)
            {
                case 0:
                    if (Model.HasSecond)
                        oxyModel
                            .CreateStepSeries(Model.First.Magnitudes(), "First")
                            .CreateStepSeries(Model.Second!.Magnitudes(), "Second");
                    else
                        oxyModel.CreateStepSeries(Model.First.Magnitudes());
                    break;
                case 1:
                    if (Model.HasSecond)
                        oxyModel
                            .CreateStepSeries(Model.First.Phases(), "First")
                            .CreateStepSeries(Model.Second!.Phases(), "Second");
                    else
                        oxyModel.CreateStepSeries(Model.First.Phases());
                    break;
                case 2:
                    var (firstRe, _) = Model.First;
                    if (Model.HasSecond)
                    {
                        var (secondRe, _) = Model.Second!;
                        oxyModel
                            .CreateStepSeries(firstRe, "First")
                            .CreateStepSeries(secondRe, "Second");
                    }
                    else
                        oxyModel.CreateStepSeries(firstRe);
                    break;
                case 3:
                    var (_, firstIm) = Model.First;
                    if (Model.HasSecond)
                    {
                        var (_, secondIm) = Model.Second!;
                        oxyModel
                            .CreateStepSeries(firstIm, "First")
                            .CreateStepSeries(secondIm, "Second");
                    }
                    else
                        oxyModel.CreateStepSeries(firstIm);
                    break;
            }
            oxyModel.ResetAllAxes();
            oxyModel.InvalidatePlot(true);
        };
        toolBar.Children.Add(combo);
        panel.Children.Add(toolBar);
        if (Model.HasSecond)
            panel.Children.Add(oxyModel
                .CreateLegend()
                .CreateStepSeries(Model.First.Magnitudes(), "First")
                .CreateStepSeries(Model.Second!.Magnitudes(), "Second")
                .CreateView());
        else
            panel.Children.Add(oxyModel
                .CreateLegend()
                .CreateStepSeries(Model.First.Magnitudes())
                .CreateView());
        return panel;
    }
}
