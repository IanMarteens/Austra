namespace Ostara;

public sealed class FftNode : VarNode
{
    public FftNode(ClassNode? parent, string varName, string formula, FftModel value) :
        base(parent, varName, formula, typeof(Series<int>))
    {
        Name = varName;
        TypeName = "FFT";
        Model = value;
    }

    public FftNode(ClassNode? parent, string varName, FftModel value) :
        this(parent, varName, varName, value)
    { }

    public override string DisplayName => $"{VarName}: {TypeName}";

    public override void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
        });
        OxyPlot.Series.StairStepSeries stepSeries = new();
        int idx = 0;
        foreach (double p in Model.Amplitudes)
            stepSeries.Points.Add(new(idx++, p));
        model.Series.Add(stepSeries);
        OxyPlot.Wpf.PlotView view = new()
        {
            Model = model,
            Width = 900,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
        };
        RootModel.Instance.AppendControl(Formula, Model.ToShortString(), view);
    }

    public FftModel Model { get; }

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }

    [Category("Stats")]
    public long Count => Model.Length;
}
