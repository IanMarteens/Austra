namespace Ostara;

public abstract class ARNode<M, T> : VarNode where M : ARModelBase<T>
{
    protected ARNode(ClassNode? parent, string varName, string formula, M value) :
        base(parent, varName, formula, value.GetType())
    {
        Name = varName;
        TypeName = "AR(p) model";
        Model = value;
        Degree = value.Degrees;
        R2 = value.R2;
        RSS = value.ResidualSumSquares;
        TSS = value.TotalSumSquares;
    }

    protected ARNode(ClassNode? parent, string varName, M value) :
        this(parent, varName, varName, value)
    { }

    public M Model { get; }

    public sealed override string DisplayName => $"{VarName}: ARModel";

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }

    [Category("Shape")]
    public int Degree { get; }

    [Category("Stats")]
    public double R2 { get; }

    [Category("Stats")]
    public double RSS { get; }

    [Category("Stats")]
    public double TSS { get; }
}

public sealed class ARSNode : ARNode<ARSModel, Series>
{
    public ARSNode(ClassNode? parent, string varName, string formula, ARSModel value) :
        base(parent, varName, formula, value)
    {
    }

    public ARSNode(ClassNode? parent, string varName, ARSModel value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        OxyPlot.Series.LineSeries lineSeries1 = new();
        foreach (Point<Date> p in Model.Original.Points)
            lineSeries1.Points.Add(
                new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
        model.Series.Add(lineSeries1);
        OxyPlot.Series.LineSeries lineSeries2 = new();
        foreach (Point<Date> p in Model.Prediction.Points)
            lineSeries2.Points.Add(
                new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
        model.Series.Add(lineSeries2);
        OxyPlot.Wpf.PlotView view = new()
        {
            Model = model,
            Width = 900,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
        };
        RootModel.Instance.AppendControl(Name, Model.ToString(), view);
    }
}

public sealed class ARVNode : ARNode<ARVModel, RVector>
{
    public ARVNode(ClassNode? parent, string varName, string formula, ARVModel value) :
        base(parent, varName, formula, value)
    {
    }

    public ARVNode(ClassNode? parent, string varName, ARVModel value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        OxyPlot.Series.LineSeries lineSeries1 = new();
        int idx = 0;
        foreach (double v in Model.Original)
            lineSeries1.Points.Add(
                new(idx++, v));
        model.Series.Add(lineSeries1);
        OxyPlot.Series.LineSeries lineSeries2 = new();
        idx = 0;
        foreach (double v in Model.Prediction)
            lineSeries2.Points.Add(
                new(idx++, v));
        model.Series.Add(lineSeries2);
        OxyPlot.Wpf.PlotView view = new()
        {
            Model = model,
            Width = 900,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
        };
        RootModel.Instance.AppendControl(Name, Model.ToString(), view);
    }
}
