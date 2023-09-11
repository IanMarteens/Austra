﻿namespace Ostara;

/// <summary>Compares two time series.</summary>
public sealed class CompareNode : VarNode<Tuple<Series, Series>>
{
    public CompareNode(ClassNode? parent, string varName, string formula, Tuple<Series, Series> value) :
        base(parent, varName, formula, "Series comparison", value)
    {
        int count = Math.Min(value.Item1.Count, value.Item2.Count);
        First = value.Item1.Prune(count);
        Second = value.Item2.Prune(count);
    }

    public CompareNode(ClassNode? parent, string varName, Tuple<Series, Series> value) :
        this(parent, varName, varName, value)
    { }

    public override void Show()
    {
        OxyPlot.PlotModel model = new();
        model.Legends.Add(new OxyPlot.Legends.Legend());
        model.Axes.Add(new OxyPlot.Axes.DateTimeAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Bottom,
        });
        model.Axes.Add(new OxyPlot.Axes.LinearAxis()
        {
            Position = OxyPlot.Axes.AxisPosition.Left,
        });
        OxyPlot.Series.LineSeries lineSeries1 = new()
        {
            Title = "First",
            TrackerFormatString = "{0}\n{1}: {2:dd/MM/yyyy}\n{3}: {4:0.####}",
        };
        foreach (Point<Date> p in First.Points)
            lineSeries1.Points.Add(
                new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
        model.Series.Add(lineSeries1);
        OxyPlot.Series.LineSeries lineSeries2 = new()
        {
            Title = "Second",
            TrackerFormatString = "{1}: {2:dd/MM/yyyy}\n{3}: {4:0.####}",
        };
        foreach (Point<Date> p in Second.Points)
            lineSeries2.Points.Add(
                new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
        model.Series.Add(lineSeries2);
        OxyPlot.Wpf.PlotView view = new()
        {
            Model = model,
            Width = 900,
            Height = 250,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black),
        };
        RootModel.Instance.AppendControl(Name, First.ToString() + " vs " + Second.ToString(), view);
    }

    public Series First { get; }
    public Series Second { get; }
}
