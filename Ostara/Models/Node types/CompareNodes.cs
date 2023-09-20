namespace Ostara;

/// <summary>Compares two time series.</summary>
public sealed class CompareNode : VarNode<Tuple<Series, Series>>
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

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/compare.png";

    public override void Show() =>
        RootModel.Instance.AppendControl(Name,
            First.ToString() + " vs " + Second.ToString(),
            CreateOxyModel(new OxyPlot.Axes.DateTimeAxis(), showLegend: true)
                .CreateSeries(First, "First")
                .CreateSeries(Second, "Second")
                .CreateView());

    public Series First { get; }
    public Series Second { get; }
}
