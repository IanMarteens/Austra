namespace Austra;

public sealed class FftNode : VarNode<FftModel>
{
    public FftNode(string formula, FftModel value) :
        base(formula, value)
    { }

    public FftNode(ClassNode parent, string varName, FftModel value) :
        base(parent, varName, value)
    { }

    public override string TypeName => "FFT";

    public override void Show() =>
        RootModel.Instance.AppendControl(
            Formula, Model.ToShortString(),
            CreateOxyModel()
            .CreateLegend()
            .CreateStepSeries(Model.Amplitudes, "Amplitudes")
            .CreateStepSeries(Model.Phases, "Phases", true)
            .CreateView());

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/waves.png";

    [Category("Stats")]
    public long Count => Model.Length;
}
