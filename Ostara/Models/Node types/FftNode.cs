namespace Ostara;

public sealed class FftNode : VarNode<FftModel>
{
    public FftNode(ClassNode? parent, string varName, string formula, FftModel value) :
        base(parent, varName, formula, "FFT", value)
    { }
    public FftNode(ClassNode? parent, string varName, FftModel value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        OxyPlot.PlotModel model = CreateOxyModel();
        OxyPlot.Series.StairStepSeries stepSeries = new();
        int idx = 0;
        foreach (double p in Model.Amplitudes)
            stepSeries.Points.Add(new(idx++, p));
        model.Series.Add(stepSeries);
        RootModel.Instance.AppendControl(Formula, Model.ToShortString(), model.CreateView());
    }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/waves.png";

    [Category("Stats")]
    public long Count => Model.Length;
}
