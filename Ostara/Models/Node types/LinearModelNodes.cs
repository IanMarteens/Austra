namespace Ostara;

public abstract class LinearModelNode<M, T> : VarNode<M> where M : LinearModelBase<T>
{
    protected LinearModelNode(ClassNode? parent, string varName, string formula, M value) :
        base(parent, varName, formula, "Linear model", value)
    {
        R2 = value.R2;
        RSS = value.ResidualSumSquares;
        TSS = value.TotalSumSquares;
        StandardError = value.StandardError;
    }

    protected void Show(OxyPlot.PlotModel oxyModel)
    {
        OxyPlot.Wpf.PlotView view = new()
        {
            Model = oxyModel,
            Width = 900,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
        };
        string text = Model.ToString();
        if (text.EndsWith("\r\n"))
            text = text[..^2];
        RootModel.Instance.AppendControl(Formula, text, view);
    }

    [Category("Stats")]
    public double R2 { get; }

    [Category("Stats")]
    public double RSS { get; }

    [Category("Stats")]
    public double TSS { get; }

    [Category("Stats")]
    public double StandardError { get; }
}

public sealed class LinearSModelNode : LinearModelNode<LinearSModel, Series>
{
    public LinearSModelNode(ClassNode? parent, string varName, string formula, LinearSModel value) :
        base(parent, varName, formula, value)
    {
    }

    public LinearSModelNode(ClassNode? parent, string varName, LinearSModel value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Legends.Add(new OxyPlot.Legends.Legend());
        model.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        OxyPlot.Series.LineSeries lineSeries1 = new() { Title = "Original" };
        foreach (Point<Date> p in Model.Original.Points)
            lineSeries1.Points.Add(
                new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
        model.Series.Add(lineSeries1);
        OxyPlot.Series.LineSeries lineSeries2 = new() { Title = "Predicted" };
        foreach (Point<Date> p in Model.Prediction.Points)
            lineSeries2.Points.Add(
                new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
        model.Series.Add(lineSeries2);
        Show(model);
    }
}

public sealed class LinearVModelNode : LinearModelNode<LinearVModel, RVector>
{
    public LinearVModelNode(ClassNode? parent, string varName, string formula, LinearVModel value) :
        base(parent, varName, formula, value)
    {
    }

    public LinearVModelNode(ClassNode? parent, string varName, LinearVModel value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Legends.Add(new OxyPlot.Legends.Legend());
        model.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        OxyPlot.Series.LineSeries lineSeries1 = new() { Title = "Original" };
        int idx = 0;
        foreach (double v in Model.Original)
            lineSeries1.Points.Add(new(idx++, v));
        model.Series.Add(lineSeries1);
        OxyPlot.Series.LineSeries lineSeries2 = new() { Title = "Predicted" };
        idx = 0;
        foreach (double v in Model.Prediction)
            lineSeries2.Points.Add(new(idx++, v));
        model.Series.Add(lineSeries2);
        Show(model);
    }
}