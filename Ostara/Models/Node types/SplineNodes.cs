namespace Ostara;

/// <summary>Common base class for nodes representing a spline model.</summary>
/// <typeparam name="T">The spline type.</typeparam>
/// <typeparam name="A">The type of the argument of the spline.</typeparam>
public abstract class SplineNode<T, A> : VarNode
    where T : Spline<A>
    where A : struct
{
    private string newValue = "";
    private string newDerivative = "";
    private A austraArg = default;
    private Poly? selected;

    protected SplineNode(ClassNode? parent, string varName, string formula, string typeName, T value) :
        base(parent, varName, formula, typeof(T))
    {
        Name = varName;
        TypeName = typeName;
        Length = value.Length;
        Spline = value;
        Coefficients = Enumerable.Range(0, Length).Select(i => new Poly(Spline, i)).ToList();
    }

    public sealed override string DisplayName => $"{VarName}: {Type.Name}";

    public List<Poly> Coefficients { get; }

    public Poly? SelectedPoly
    {
        get => selected;
        set => SetField(ref selected, value);
    }

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }

    [Category("Shape")]
    public int Length { get; }

    public T Spline { get; }

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
            SelectedPoly = Coefficients[Spline.NearestArg(arg)];
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
        base(parent, varName, formula, "Date spline", value) =>
        NewDate = (DateTime)Coefficients[0].From;

    public DateSplineNode(ClassNode? parent, string varName, DateSpline value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    { 
        if (OxyModel == null)
        {
            OxyModel = new();
            OxyModel.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
            });
            OxyModel.Axes.Add(new OxyPlot.Axes.LinearAxis()
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
            });
            OxyPlot.Series.LineSeries lineSeries = new();
            foreach (Point<Date> p in Series.Points)
                lineSeries.Points.Add(
                    new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
            OxyModel.Series.Add(lineSeries);
        }
        RootModel.Instance.AppendControl(Name, Series.ToString(), new SSplineView() { DataContext = this });
    }

    public Series Series => Spline.Original;

    public OxyPlot.PlotModel? OxyModel { get; private set; }

    public DateTime NewDate
    {
        get => newDate;
        set
        {
            if (SetField(ref newDate, value))
            {
                SelectPoly((double)(Date)newDate);
                try
                {
                    NewValue = Spline[(Date)newDate].ToString("G8");
                    NewDerivative = Spline.Derivative((Date)newDate).ToString("G8");
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

