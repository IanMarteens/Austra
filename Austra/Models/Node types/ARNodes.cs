namespace Austra;

/// <summary>An abstract autoregressive model.</summary>
/// <typeparam name="M">Type of model.</typeparam>
/// <typeparam name="T">Type of original dataset.</typeparam>
public abstract class ARNode<M, T> : VarNode<M> where M : ARModel<T>
{
    protected ARNode(string formula, M value) :
        base(formula, value) =>
        (Degree, R2, RSS, TSS) =
            (value.Degrees, value.R2, value.ResidualSumSquares, value.TotalSumSquares);

    protected ARNode(ClassNode? parent, string varName, M value) :
        base(parent, varName, value) =>
        (Degree, R2, RSS, TSS) =
            (value.Degrees, value.R2, value.ResidualSumSquares, value.TotalSumSquares);

    public override string TypeName => "AR(p) model";

    protected void Show(OxyPlot.PlotModel oxyModel)
    {
        string text = Model.ToString();
        if (text.EndsWith("\r\n"))
            text = text[..^2];
        RootModel.Instance.AppendControl(Formula, text, oxyModel.CreateView());
    }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/model.png";

    [Category("Shape")]
    public int Degree { get; }

    [Category("Stats")]
    public double R2 { get; }

    [Category("Stats")]
    public double RSS { get; }

    [Category("Stats")]
    public double TSS { get; }
}

/// <summary>An autoregressive model for a time series.</summary>
public sealed class ARSNode : ARNode<ARSModel, Series>
{
    public ARSNode(string formula, ARSModel value) :
        base(formula, value)
    { }

    public ARSNode(ClassNode parent, string varName, ARSModel value) :
        base(parent, varName, value)
    { }

    public override void Show() => Show(
        CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
}

/// <summary>An autoregressive model for a samples in a vector.</summary>
public sealed class ARVNode : ARNode<ARVModel, RVector>
{
    public ARVNode(string formula, ARVModel value) :
        base(formula, value)
    { }

    public ARVNode(ClassNode? parent, string varName, ARVModel value) :
        base(parent, varName, value)
    { }

    public override void Show() => Show(
        CreateOxyModel()
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
}
