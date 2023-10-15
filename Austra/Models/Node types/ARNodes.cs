namespace Austra;

/// <summary>An abstract autoregressive model.</summary>
/// <typeparam name="M">Type of model.</typeparam>
/// <typeparam name="T">Type of original dataset.</typeparam>
public abstract class ARNode<M, T> : VarNode<M> where M : ARModelBase<T>
{
    protected ARNode(ClassNode? parent, string varName, string formula, M value) :
        base(parent, varName, formula, "AR(p) model", value)
    {
        Degree = value.Degrees;
        R2 = value.R2;
        RSS = value.ResidualSumSquares;
        TSS = value.TotalSumSquares;
    }

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
    public ARSNode(ClassNode? parent, string varName, string formula, ARSModel value) :
        base(parent, varName, formula, value)
    {
    }

    public ARSNode(ClassNode? parent, string varName, ARSModel value) :
        this(parent, varName, varName, value)
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
    public ARVNode(ClassNode? parent, string varName, string formula, ARVModel value) :
        base(parent, varName, formula, value)
    {
    }

    public ARVNode(ClassNode? parent, string varName, ARVModel value) :
        this(parent, varName, varName, value)
    { }

    public override void Show() => Show(
        CreateOxyModel()
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
}
