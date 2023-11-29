namespace Austra;

public abstract class LinearModelNode<M, T> : VarNode<M> where M : LinearModel<T>
{
    protected LinearModelNode(string formula, M value) :
        base(formula, value) =>
        (R2, RSS, TSS, StandardError) =
            (value.R2, value.ResidualSumSquares, value.TotalSumSquares, value.StandardError);

    protected LinearModelNode(ClassNode parent, string varName, M value) :
        base(parent, varName, value) =>
        (R2, RSS, TSS, StandardError) =
            (value.R2, value.ResidualSumSquares, value.TotalSumSquares, value.StandardError);

    public override string TypeName => "Linear model";

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
    public LinearSModelNode(string formula, LinearSModel value) :
        base(formula, value)
    { }

    public LinearSModelNode(ClassNode parent, string varName, LinearSModel value) :
        base(parent, varName, value)
    { }

    public override void Show() => Show(
        CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
}

public sealed class LinearVModelNode : LinearModelNode<LinearVModel, DVector>
{
    public LinearVModelNode(string formula, LinearVModel value) :
        base(formula, value)
    { }

    public LinearVModelNode(ClassNode parent, string varName, LinearVModel value) :
        base(parent, varName, value)
    { }

    public override void Show() => Show(
        CreateOxyModel()
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
}