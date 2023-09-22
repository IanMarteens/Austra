namespace Ostara;

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
            CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
                .CreateLegend()
                .CreateStepSeries(Model.Item1, "First")
                .CreateStepSeries(Model.Item2, "Second")
                .CreateView());
}
