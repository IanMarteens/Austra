using Austra.Parser;

namespace Austra;

/// <summary>Represents a node in the entities tree.</summary>
/// <param name="name">The name of the node.</param>
public abstract class NodeBase(string name) : Entity
{
    private bool isSelected;
    private bool isExpanded;

    /// <summary>Gets or set whether the corresponding tree node is selected.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetField(ref isSelected, value);
    }

    /// <summary>Gets or set whether the corresponding tree node is expanded.</summary>
    public bool IsExpanded
    {
        get => isExpanded;
        set => SetField(ref isExpanded, value);
    }

    /// <summary>Shows the corresponding view in the main window.</summary>
    public virtual void Show() { }

    [Category("ID")]
    public string Name { get; } = name;

    [Category("ID")]
    public abstract string TypeName { get; }
}

/// <summary>Represents a class node, grouping variables of the same type.</summary>
/// <param name="className">Descriptive name of the class.</param>
/// <param name="type">The type of class node.</param>
public class ClassNode(string className, string type = "Class node") : NodeBase(className)
{
    public ObservableCollection<VarNode> Nodes { get; } = [];

    public override string TypeName { get; } = type;

    public int Order { get; } = className switch
        {
            "Series" => 0,
            "Matrix" => 1,
            "Vector" => 2,
            _ => 3,
        };
}

/// <summary>Represents an class node grouping AUSTRA definitions.</summary>
public class AllDefinitionsNode : ClassNode
{
    public AllDefinitionsNode() : base("Definitions", "Definition node") { }
}

/// <summary>Represents a session variable.</summary>
public abstract class VarNode : NodeBase
{
    protected VarNode(ClassNode? parent, string name, string formula) :
        base(name) =>
        (Parent, Formula) = (parent, formula);

    public ClassNode? Parent { get; }
    /// <summary>Gets the expression that yields the value of the variable.</summary>
    public string Formula { get; }

    public bool Stored { get; init; }

    public virtual Visibility ImageVisibility =>
        Stored ? Visibility.Visible : Visibility.Collapsed;

    public virtual string ImageSource => Stored ? "/images/store.png" : "/images/tag.png";

    public Visibility IsOrphan =>
        Parent != null ? Visibility.Collapsed : Visibility.Visible;

    protected virtual string GetExcelText() => "";

    public virtual string Hint => $"{Name}: {TypeName}";

    public static OxyPlot.PlotModel CreateOxyModel(
        OxyPlot.Axes.Axis? xAxis = null, OxyPlot.Axes.Axis? yAxis = null)
    {
        OxyPlot.PlotModel model = new();
        xAxis ??= new OxyPlot.Axes.LinearAxis();
        xAxis.Position = OxyPlot.Axes.AxisPosition.Bottom;
        if (xAxis is OxyPlot.Axes.DateTimeAxis)
            xAxis.StringFormat = "dd/MM/yyyy";
        model.Axes.Add(xAxis);
        yAxis ??= new OxyPlot.Axes.LinearAxis();
        yAxis.Position = OxyPlot.Axes.AxisPosition.Left;
        model.Axes.Add(yAxis);
        return model;
    }
}

/// <summary>Represents a session variable with a stored value.</summary>
public abstract class VarNode<T> : VarNode
{
    protected VarNode(string formula, T model) :
        base(null, model?.GetType()?.Name ?? "", formula) => Model = model;

    protected VarNode(ClassNode? parent, string varName, T model) :
        base(parent, varName, varName) => Model = model;

    /// <summary>Gets the value associated to the linked variable.</summary>
    public T Model { get; }

    /// <summary>Shows the formula and the text of the result in the main window.</summary>
    public override void Show() => RootModel.Instance.AppendResult(Formula, Model!.ToString());
}

/// <summary>Extension methods for OxyPlot models.</summary>
internal static class OxyExts
{
    public static OxyPlot.Wpf.PlotView CreateView(
        this OxyPlot.PlotModel model, int width = 900, int height = 250) => new MyPlotView()
        {
            Model = model,
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black)
        };

    public sealed class MyPlotView : OxyPlot.Wpf.PlotView
    {
        protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.ChangedButton == MouseButton.Left)
            {
                Model?.ResetAllAxes();
                Model?.InvalidatePlot(false);
            }
        }
    }

    public static OxyPlot.PlotModel CreateLegend(this OxyPlot.PlotModel model)
    {
        model.Legends.Add(new OxyPlot.Legends.Legend());
        return model;
    }

    public static OxyPlot.PlotModel CreateLine(
        this OxyPlot.PlotModel model, double value)
    {
        model.Annotations.Add(new OxyPlot.Annotations.LineAnnotation()
        {
            Type = OxyPlot.Annotations.LineAnnotationType.Vertical,
            Color = OxyPlot.OxyColors.RoyalBlue,
            LineStyle = OxyPlot.LineStyle.Solid,
            StrokeThickness = 1,
            X = value,
        });
        return model;
    }

    public static OxyPlot.PlotModel CreateSeries(
        this OxyPlot.PlotModel model, Series series, string title = "")
    {
        OxyPlot.Series.LineSeries lineSeries = new()
        {
            TrackerFormatString = "{0}\n{1}: {2:dd/MM/yyyy}\n{3}: {4:0.####}",
        };
        if (title != "")
            lineSeries.Title = title;
        foreach (Point<Date> p in series.Points)
            lineSeries.Points.Add(
                new(OxyPlot.Axes.Axis.ToDouble((DateTime)p.Arg), p.Value));
        model.Series.Add(lineSeries);
        return model;
    }

    public static OxyPlot.PlotModel CreateSeries(
        this OxyPlot.PlotModel model, DVector vector, string title = "")
    {
        OxyPlot.Series.LineSeries lineSeries = new()
        {
            TrackerFormatString = "{0}\n{1}: {2:#0}\n{3}: {4:0.####}",
        };
        if (title != "")
            lineSeries.Title = title;
        for (int i = 0; i < vector.Length; i++)
            lineSeries.Points.Add(new(i, vector[i]));
        model.Series.Add(lineSeries);
        return model;
    }

    public static OxyPlot.PlotModel CreateSeries(
        this OxyPlot.PlotModel model, Series<double> series)
    {
        OxyPlot.Series.LineSeries lineSeries = new()
        {
            TrackerFormatString = "{1}: {2:0.####}\n{3}: {4:0.####}",
        };
        foreach (Point<double> p in series.Points)
            lineSeries.Points.Add(new(p.Arg, p.Value));
        model.Series.Add(lineSeries);
        return model;
    }

    public static OxyPlot.PlotModel CreateStepSeries(
        this OxyPlot.PlotModel model, DVector vector, string? title = null, bool hidden = false)
    {
        OxyPlot.Series.StairStepSeries stepSeries = new()
        {
            TrackerFormatString = "{1}: {2:0.####}\n{3}: {4:0.####}",
            IsVisible = !hidden,
        };
        if (!string.IsNullOrEmpty(title))
            stepSeries.Title = title;
        for (int i = 0; i < vector.Length; i++)
            stepSeries.Points.Add(new(i, vector[i]));
        model.Series.Add(stepSeries);
        return model;
    }

    public static void UpdateLine(this OxyPlot.PlotModel model, double value)
    {
        if (model.Annotations.Count > 0)
        {
            ((OxyPlot.Annotations.LineAnnotation)model.Annotations[0]).X = value;
            model.InvalidatePlot(false);
        }
    }
}

/// <summary>A catch-all variable node, for variables that are not of a specific type.</summary>
public class MiscNode(ClassNode parent, string varName, Type type, string value) :
    VarNode(parent, varName, varName)
{
    public override string TypeName { get; } = type.Name;

    public override void Show()
    {
        if (Parent != null)
        {
            Parent.IsExpanded = true;
            IsSelected = true;
        }
        RootModel.Instance.AppendResult(Name, Value);
    }

    public override Visibility ImageVisibility => Visibility.Visible;

    [Category("Content")]
    public string Value { get; } = value;
}

public sealed class DefinitionNode(AllDefinitionsNode parent, Definition def) :
    VarNode(parent, def.Name, def.Name)
{
    public override string TypeName { get; } =
        def.Type == typeof(ARSModel) || def.Type == typeof(ARVModel)
        ? "ARModel"
        : def.Type == typeof(LinearSModel) || def.Type == typeof(LinearVModel)
        ? "LinearModel"
        : def.Type == typeof(FftCModel) || def.Type == typeof(FftRModel)
        ? "FFT Model"
        : def.Type.Name;

    override public void Show() =>
        RootModel.Instance.Evaluate(Name);

    [Category("Content")]
    public string Body { get; } = def.Text;

    [Category("Content")]
    public string Description { get; } = def.Description;

    public override string Hint => (Description == Name ? TypeName : Description) + Environment.NewLine + Body;
}
