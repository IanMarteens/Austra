namespace Ostara;

/// <summary>Base class for comparing series and vectors.</summary>
/// <typeparam name="T">The type of the compared items.</typeparam>
public abstract class CompareNodeBase<T> : VarNode<Tuple<T, T>>
{
    protected CompareNodeBase(ClassNode? parent, string name, string formula, string type, Tuple<T, T> model)
        : base(parent, name, formula, type, model)
    { }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/compare.png";
}

/// <summary>Compares two time series.</summary>
public sealed class CompareNode : CompareNodeBase<Series>
{
    public CompareNode(ClassNode? parent, string varName, string formula, Tuple<Series, Series> value) :
        base(parent, varName, formula, "Series comparison", value)
    {
        int count = Math.Min(value.Item1.Count, value.Item2.Count);
        First = value.Item1.Prune(count);
        Second = value.Item2.Prune(count);
    }

    public CompareNode(ClassNode? parent, string varName, Tuple<Series, Series> value) :
        this(parent, varName, varName, value)
    { }

    public override void Show() =>
        RootModel.Instance.AppendControl(Name,
            First.ToString() + " vs " + Second.ToString(),
            CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
                .CreateLegend()
                .CreateSeries(First, "First")
                .CreateSeries(Second, "Second")
                .CreateView());

    public Series First { get; }
    public Series Second { get; }
}

public sealed class CompareVNode : CompareNodeBase<RVector>
{
    public CompareVNode(ClassNode? parent, string varName, string formula, Tuple<RVector, RVector> value) :
        base(parent, varName, formula, "Series comparison", value)
    {
    }

    public CompareVNode(ClassNode? parent, string varName, Tuple<RVector, RVector> value) :
            this(parent, varName, varName, value)
    { }

    public override void Show() =>
        RootModel.Instance.AppendControl(Name,
            $"ℝ({Model.Item1.Length}) vs ℝ({Model.Item2.Length})",
            CreateOxyModel(new OxyPlot.Axes.LinearAxis())
                .CreateLegend()
                .CreateStepSeries(Model.Item1, "First")
                .CreateStepSeries(Model.Item2, "Second")
                .CreateView());
}

public sealed class CompareCVNode : CompareNodeBase<ComplexVector>
{
    public CompareCVNode(ClassNode? parent, string varName, string formula,
        Tuple<ComplexVector, ComplexVector> value) :
        base(parent, varName, formula, "Series comparison", value)
    {
    }

    public CompareCVNode(ClassNode? parent, string varName,
        Tuple<ComplexVector, ComplexVector> value) :
            this(parent, varName, varName, value)
    { }

    public override void Show() =>
        RootModel.Instance.AppendControl(Name,
            $"ℂ({Model.Item1.Length}) vs ℂ({Model.Item2.Length})",
            CreateControl());

    private UIElement CreateControl()
    {
        OxyPlot.PlotModel oxyModel = CreateOxyModel(new OxyPlot.Axes.LinearAxis());
        StackPanel panel = new() { Orientation = Orientation.Vertical };
        StackPanel toolBar = new() { Orientation = Orientation.Horizontal };
        toolBar.Children.Add(new Label()
        {
            Content = "Compare:",
            VerticalAlignment = VerticalAlignment.Center,
        });
        List<string> references = new() { "Magnitude", "Phase", "Real", "Imaginary" };
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
                    oxyModel
                        .CreateStepSeries(Model.Item1.Magnitudes(), "First")
                        .CreateStepSeries(Model.Item2.Magnitudes(), "Second");
                    break;
                case 1:
                    oxyModel
                        .CreateStepSeries(Model.Item1.Phases(), "First")
                        .CreateStepSeries(Model.Item2.Phases(), "Second");
                    break;
                case 2:
                    var (firstRe, _) = Model.Item1;
                    var (secondRe, _) = Model.Item2;
                    oxyModel
                        .CreateStepSeries(firstRe, "First")
                        .CreateStepSeries(secondRe, "Second");
                    break;
                case 3:
                    var (_, firstIm) = Model.Item1;
                    var (_, secondIm) = Model.Item2;
                    oxyModel
                        .CreateStepSeries(firstIm, "First")
                        .CreateStepSeries(secondIm, "Second");
                    break;
            }
            oxyModel.ResetAllAxes();
            oxyModel.InvalidatePlot(true);
        };  
        toolBar.Children.Add(combo);
        panel.Children.Add(toolBar);
        panel.Children.Add(oxyModel
                .CreateLegend()
                .CreateStepSeries(Model.Item1.Magnitudes(), "First")
                .CreateStepSeries(Model.Item2.Magnitudes(), "Second")
                .CreateView());
        return panel;
    }
}
