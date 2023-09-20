﻿using OxyPlot;
using OxyPlot.Annotations;

namespace Ostara;

/// <summary>Common base class for nodes representing a spline model.</summary>
/// <typeparam name="T">The spline type.</typeparam>
/// <typeparam name="A">The type of the argument of the spline.</typeparam>
public abstract class SplineNode<T, A> : VarNode<T>
    where T : Spline<A>
    where A : struct
{
    private string newValue = "";
    private string newDerivative = "";
    private A austraArg = default;
    private Poly? selected;

    protected SplineNode(ClassNode? parent, string varName, string formula, T value) :
        base(parent, varName, formula, "Spline", value)
    {
        Length = value.Length;
        Coefficients = Enumerable.Range(0, Length).Select(i => new Poly(Model, i)).ToList();
    }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/model.png";

    public OxyPlot.PlotModel? OxyModel { get; protected set; }

    public List<Poly> Coefficients { get; }

    public Poly? SelectedPoly
    {
        get => selected;
        set => SetField(ref selected, value);
    }

    [Category("Shape")]
    public int Length { get; }

    public string NewValue
    {
        get => newValue;
        set => SetField(ref newValue, value);
    }

    public string NewDerivative
    {
        get => newDerivative;
        set => SetField(ref newDerivative, value);
    }

    public A AustraArg
    {
        get => austraArg;
        set => SetField(ref austraArg, value);
    }

    protected void SelectPoly(double arg)
    {
        try
        {
            SelectedPoly = Coefficients[Model.NearestArg(arg)];
            if (SelectedPoly != null)
                AustraArg = SelectedPoly.From;
        }
        catch { }
    }

    public sealed record class Poly(
        A From, A To,
        double K0, double K1, double K2, double K3)
    {
        public Poly(Spline<A> spline, int idx) : this(
            spline.From(idx), spline.To(idx),
                spline.K[idx].K0, spline.K[idx].K1, spline.K[idx].K2, spline.K[idx].K3)
        { }
    }
}

public sealed class DateSplineNode : SplineNode<DateSpline, Date>
{
    private DateTime newDate;

    public DateSplineNode(ClassNode? parent, string varName, string formula, DateSpline value) :
        base(parent, varName, formula, value) =>
        NewDate = (DateTime)Coefficients[0].From;

    public DateSplineNode(ClassNode? parent, string varName, DateSpline value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        OxyModel ??= CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
            .CreateLine(OxyPlot.Axes.Axis.ToDouble(NewDate))
            .CreateSeries(Series);
        RootModel.Instance.AppendControl(Formula, Series.ToString(), new DSplineView() { DataContext = this });
    }

    public Series Series => Model.Original;

    public DateTime NewDate
    {
        get => newDate;
        set
        {
            if (SetField(ref newDate, value))
            {
                OxyModel?.UpdateLine(OxyPlot.Axes.Axis.ToDouble(newDate));
                SelectPoly((double)(Date)newDate);
                try
                {
                    NewValue = Model[(Date)newDate].ToString("G8");
                    NewDerivative = Model.Derivative((Date)newDate).ToString("G8");
                }
                catch (Exception ex)
                {
                    NewValue = ex.Message;
                    NewDerivative = "";
                }
            }
        }
    }
}

public sealed class VectorSplineNode : SplineNode<VectorSpline, double>
{
    private decimal newArg = decimal.MaxValue;

    public VectorSplineNode(ClassNode? parent, string varName, string formula, VectorSpline value) :
        base(parent, varName, formula, value) =>
        NewArg = (decimal)Coefficients[0].From;

    public VectorSplineNode(ClassNode? parent, string varName, VectorSpline value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        OxyModel ??= CreateOxyModel().CreateLine((double)NewArg)
            .CreateSeries(Series);
        RootModel.Instance.AppendControl(Formula, Series.ToString(), new VSplineView() { DataContext = this });
    }

    public Series<double> Series => Model.Original;

    public decimal NewArg
    {
        get => newArg;
        set
        {
            if (SetField(ref newArg, value))
            {
                OxyModel?.UpdateLine((double)newArg);
                SelectPoly((double)newArg);
                try
                {
                    NewValue = Model[(double)newArg].ToString("G8");
                    NewDerivative = Model.Derivative((double)newArg).ToString("G8");
                }
                catch (Exception ex)
                {
                    NewValue = ex.Message;
                    NewDerivative = "";
                }
            }
        }
    }
}
