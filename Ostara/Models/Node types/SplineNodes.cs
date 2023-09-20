namespace Ostara;

/// <summary>Common base class for nodes representing a spline model.</summary>
/// <typeparam name="T">The spline type.</typeparam>
/// <typeparam name="A">The type of the argument of the spline.</typeparam>
public abstract class SplineNode<T, A> : VarNode<T>
    where T : Spline<A>
    where A : struct
{
    protected SplineNode(ClassNode? parent, string varName, string formula, T value) :
        base(parent, varName, formula, "Spline", value) => Length = value.Length;

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/model.png";

    [Category("Shape")]
    public int Length { get; }
}

public abstract class SplineViewModel<T, A> : Entity
    where T : Spline<A>
    where A : struct
{
    private string newValue = "";
    private string newDerivative = "";
    private A austraArg = default;
    private Poly? selected;

    protected SplineViewModel(SplineNode<T, A> node) => (Node, Coefficients)
        = (node, Enumerable.Range(0, node.Length).Select(i => new Poly(node.Model, i)).ToList());

    public SplineNode<T, A> Node { get; }

    public List<Poly> Coefficients { get; }

    public OxyPlot.PlotModel? OxyModel { get; protected set; }

    public Poly? SelectedPoly
    {
        get => selected;
        set => SetField(ref selected, value);
    }

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
            SelectedPoly = Coefficients[Node.Model.NearestArg(arg)];
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
    public DateSplineNode(ClassNode? parent, string varName, string formula, DateSpline value) :
        base(parent, varName, formula, value)
    { }

    public DateSplineNode(ClassNode? parent, string varName, DateSpline value) :
        this(parent, varName, varName, value)
    { }

    public override void Show() =>
        RootModel.Instance.AppendControl(Formula, Model.Original.ToString(),
            new DSplineView() { DataContext = new DateSplineViewModel(this) });
}

public sealed class DateSplineViewModel : SplineViewModel<DateSpline, Date>
{
    private DateTime newDate;

    public DateSplineViewModel(DateSplineNode node) : base(node)
    {
        NewDate = (DateTime)Coefficients[0].From;
        OxyModel = VarNode.CreateOxyModel(new OxyPlot.Axes.DateTimeAxis())
            .CreateLine(OxyPlot.Axes.Axis.ToDouble(NewDate))
            .CreateSeries(Node.Model.Original);
    }

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
                    NewValue = Node.Model[(Date)newDate].ToString("G8");
                    NewDerivative = Node.Model.Derivative((Date)newDate).ToString("G8");
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
    public VectorSplineNode(ClassNode? parent, string varName, string formula, VectorSpline value) :
        base(parent, varName, formula, value)
    { }

    public VectorSplineNode(ClassNode? parent, string varName, VectorSpline value) :
        this(parent, varName, varName, value)
    { }

    public override void Show() =>
        RootModel.Instance.AppendControl(Formula, Model.Original.ToString(),
            new VSplineView() { DataContext = new VectorSplineViewModel(this) });
}

public sealed class VectorSplineViewModel : SplineViewModel<VectorSpline, double>
{
    private decimal newArg = decimal.MaxValue;

    public VectorSplineViewModel(VectorSplineNode node) : base(node)
    {
        NewArg = (decimal)Coefficients[0].From;
        OxyModel = VarNode.CreateOxyModel()
            .CreateLine((double)NewArg)
            .CreateSeries(Node.Model.Original);
    }

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
                    NewValue = Node.Model[(double)newArg].ToString("G8");
                    NewDerivative = Node.Model.Derivative((double)newArg).ToString("G8");
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
