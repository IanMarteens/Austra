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

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/linear.png";

    protected void Show(OxyPlot.PlotModel oxyModel)
    {
        string text = Model.ToString();
        if (text.EndsWith("\r\n"))
            text = text[..^2];
        RootModel.Instance.AppendControl(Formula, text, oxyModel.CreateView());
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

    public override void Show() => Show(
        CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
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

    public override void Show() => Show(
        CreateOxyModel()
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
}