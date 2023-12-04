using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Austra;

/// <summary>An abstract moving average model.</summary>
/// <typeparam name="M">Type of model.</typeparam>
/// <typeparam name="T">Type of original dataset.</typeparam>
public abstract class MANode<M, T> : VarNode<M> where M : MAModel<T>
{
    protected MANode(string formula, M value) :
        base(formula, value) =>
        (Degree, R2, RSS, TSS) =
            (value.Degrees, value.R2, value.ResidualSumSquares, value.TotalSumSquares);

    protected MANode(ClassNode? parent, string varName, M value) :
        base(parent, varName, value) =>
        (Degree, R2, RSS, TSS) =
            (value.Degrees, value.R2, value.ResidualSumSquares, value.TotalSumSquares);

    public override string TypeName => "MA(q) model";

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

/// <summary>A moving average model for a time series.</summary>
public sealed class MASNode : MANode<MASModel, Series>
{
    public MASNode(string formula, MASModel value) :
        base(formula, value)
    { }

    public MASNode(ClassNode parent, string varName, MASModel value) :
        base(parent, varName, value)
    { }

    public override void Show() => Show(
        CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
}

/// <summary>A moving average model for samples in a vector.</summary>
public sealed class MAVNode : MANode<MAVModel, DVector>
{
    public MAVNode(string formula, MAVModel value) :
        base(formula, value)
    { }

    public MAVNode(ClassNode? parent, string varName, MAVModel value) :
        base(parent, varName, value)
    { }

    public override void Show() => Show(
        CreateOxyModel()
            .CreateLegend()
            .CreateSeries(Model.Original, "Original")
            .CreateSeries(Model.Prediction, "Predicted"));
}
