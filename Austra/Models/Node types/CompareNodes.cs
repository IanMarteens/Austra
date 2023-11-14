namespace Austra;

/// <summary>Base class for comparing series and vectors.</summary>
/// <typeparam name="T">The type of the compared items.</typeparam>
public abstract class CompareNodeBase<T> : VarNode<Plot<T>> where T : IFormattable
{
    protected CompareNodeBase(ClassNode? parent, string name, string formula, string type, Plot<T> model)
        : base(parent, name, formula, type, model)
    { }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/compare.png";
}

/// <summary>Compares two time series.</summary>
public sealed class CompareNode(ClassNode? parent, string varName, string formula, Plot<Series> value)
    : CompareNodeBase<Series>(parent, varName, formula, "Series plot", value)
{
    public CompareNode(ClassNode? parent, string varName, Plot<Series> value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        if (Model.HasSecond)
            RootModel.Instance.AppendControl(Name,
                Model.First.ToString() + " vs " + Model.Second!.ToString(),
                CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
                    .CreateLegend()
                    .CreateSeries(Model.First, "First")
                    .CreateSeries(Model.Second!, "Second")
                    .CreateView());
        else
            RootModel.Instance.AppendControl(Name,
                Model.First.ToString(),
                CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
                    .CreateLegend()
                    .CreateSeries(Model.First)
                    .CreateView());
    }
}

public sealed class CompareVNode(ClassNode? parent, string varName, string formula, Plot<RVector> value)
    : CompareNodeBase<RVector>(parent, varName, formula, "Vector plot", value)
{
    public CompareVNode(ClassNode? parent, string varName, Plot<RVector> value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        if (Model.HasSecond)
            RootModel.Instance.AppendControl(Name,
                $"ℝ({Model.First.Length}) vs ℝ({Model.Second!.Length})",
                CreateOxyModel(new OxyPlot.Axes.LinearAxis())
                    .CreateLegend()
                    .CreateStepSeries(Model.First, "First")
                    .CreateStepSeries(Model.Second!, "Second")
                    .CreateView());
        else
            RootModel.Instance.AppendControl(Name,
                $"ℝ({Model.First.Length})",
                CreateOxyModel(new OxyPlot.Axes.LinearAxis())
                    .CreateLegend()
                    .CreateStepSeries(Model.First)
                    .CreateView());
    }
}

public sealed class CompareCVNode(ClassNode? parent, string varName, string formula, Plot<ComplexVector> value)
    : CompareNodeBase<ComplexVector>(parent, varName, formula, "Complex vector plot", value)
{
    public CompareCVNode(ClassNode? parent, string varName, Plot<ComplexVector> value) :
            this(parent, varName, varName, value)
    { }

    public override void Show() =>
        RootModel.Instance.AppendControl(Name,
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
