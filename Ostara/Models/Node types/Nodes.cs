using Austra.Parser;

namespace Ostara;

/// <summary>Represents a node in the entities tree.</summary>
public abstract class NodeBase : Entity
{
    private bool isSelected;
    private bool isExpanded;

    protected NodeBase(string name, string type) => (Name, TypeName) = (name, type);

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
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }
}

/// <summary>Represents a class node, grouping variables of the same type.</summary>
public class ClassNode : NodeBase
{
    public ClassNode(string className) : this(className, "ClassNode") { }

    public ClassNode(string className, string type) : base(className, type) =>
        Order = className switch
        {
            "Series" => 0,
            "Matrix" => 1,
            "Vector" => 2,
            _ => 3,
        };

    public ObservableCollection<VarNode> Nodes { get; } = new();

    public int Order { get; }
}

/// <summary>Represents an class node grouping AUSTRA definitions.</summary>
public class AllDefinitionsNode : ClassNode
{
    public AllDefinitionsNode() : base("Definitions", "Definition node") { }
}

/// <summary>Represents a session variable.</summary>
public abstract class VarNode : NodeBase
{
    protected VarNode(ClassNode? parent, string name, string formula, string type) :
        base(name, type) =>
        (Parent, Formula) = (parent, formula);

    public ClassNode? Parent { get; }
    /// <summary>Gets the expression that yields the value of the variable.</summary>
    public string Formula { get; }

    public bool Stored { get; init; }

    public virtual Visibility ImageVisibility =>
        Stored ? Visibility.Visible : Visibility.Collapsed;

    public virtual string ImageSource => Stored ? "/images/store.png" : "";

    public Visibility IsOrphan =>
        Parent != null ? Visibility.Collapsed : Visibility.Visible;

    protected virtual string GetExcelText() => "";

    public virtual string Hint => $"{Name}: {TypeName}";
}

/// <summary>Represents a session variable with a stored value.</summary>
public abstract class VarNode<T> : VarNode
{
    protected VarNode(ClassNode? parent, string name, string formula, string type, T model) :
        base(parent, name, formula, type) => Model = model;

    /// <summary>Gets the value associated to the linked variable.</summary>
    public T Model { get; }

    protected OxyPlot.PlotModel CreateOxyModel(
        OxyPlot.Axes.Axis? xAxis = null, OxyPlot.Axes.Axis? yAxis = null,
        bool showLegend = false)
    {
        OxyPlot.PlotModel model = new();
        if (showLegend)
            model.Legends.Add(new OxyPlot.Legends.Legend());
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

/// <summary>Extension methods for OxyPlot models.</summary>
internal static class OxyExts
{
    public static OxyPlot.Wpf.PlotView CreateView(
        this OxyPlot.PlotModel model, int width = 900, int height = 250) => new()
        {
            Model = model,
            Width = width,
            Height = height,
            HorizontalAlignment = HorizontalAlignment.Left,
            Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black)
        };

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
public class MiscNode : VarNode
{
    public MiscNode(ClassNode parent, string varName, Type type, string value) :
        base(parent, varName, varName, type.Name) =>
        Value = value;

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

    public override string ImageSource => "/images/tag.png";

    [Category("Content")]
    public string Value { get; }
}

public sealed class DefinitionNode : VarNode
{
    public DefinitionNode(AllDefinitionsNode parent, Definition def) :
        base(parent, def.Name, def.Name, def.Type == typeof(ARSModel) || def.Type == typeof(ARVModel)
            ? "ARModel"
            : def.Type == typeof(LinearSModel) || def.Type == typeof(LinearVModel)
            ? "LinearModel"
            : def.Type == typeof(FftCModel) || def.Type == typeof(FftRModel)
            ? "FFT Model"
            : def.Type.Name)
    {
        Body = def.Text;
        Description = def.Description;
    }

    override public void Show() =>
        RootModel.Instance.Evaluate(Name);

    [Category("Content")]
    public string Body { get; }

    [Category("Content")]
    public string Description { get; }

    public override string Hint => (Description == Name ? TypeName : Description) + Environment.NewLine + Body;
}
