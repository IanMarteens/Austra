﻿using Austra.Parser;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Ostara;

/// <summary>Represents the initial view-model of Ostara.</summary>
public sealed partial class RootModel : Entity
{
    public static RootModel Instance { get; } = new();

    /// <summary>Global transient message for the status bar.</summary>
    private string message = "";
    /// <summary>Austra session, containing variables and definitions.</summary>
    private Session? environment;
    /// <summary>The tree of variables, grouped by class. First node are definitions.</summary>
    private ObservableCollection<ClassNode> classes = new();
    /// <summary>Maps names into variable nodes.</summary>
    private readonly Dictionary<string, VarNode> allVars = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>This timer erases the status bar message after a while.</summary>
    private readonly DispatcherTimer timer = new();
    /// <summary>Last date of the series. Used to update the status bar.</summary>
    private string austraDate = "";
    /// <summary>The scroll viewer of the results panel.</summary>
    private ScrollViewer? scrollViewer;
    private string errorText = "";
    private Visibility showErrorText = Visibility.Collapsed;

    public RootModel()
    {
        CommonMatrix.TERMINAL_COLUMNS = 160;
        timer.Interval = new TimeSpan(0, 0, 15);
        timer.Tick += (e, a) => 
        { 
            ErrorText = "";
            Message = "";
            ShowErrorText = Visibility.Collapsed; 
            timer.Stop();
        };
        string dataFile = GetDefaultDataFile();
        if (File.Exists(dataFile))
        {
            Environment = new Session(dataFile);
            PrepareWorkspace();
        }
    }

    /// <summary>Gets the version of Austra.Libray.</summary>
    public static string Version { get; } =
        typeof(Series).Assembly.GetName().Version!.ToString(3);

    /// <summary>Gets or sets a message in the status bar for a time lapse.</summary>
    public string Message
    {
        get => message;
        set
        {
            if (SetField(ref message, value))
            {
                if (!string.IsNullOrEmpty(value))
                    timer.Start();
            }
        }
    }

    /// <summary>Austra session, containing variables and definitions.</summary>
    public Session? Environment
    {
        get => environment;
        set => SetField(ref environment, value, nameof(HasEnvironment));
    }

    public bool HasEnvironment => environment != null;

    public bool GetHasEnvironment() => environment != null;

    /// <summary>Gets the last date in time series, in Austra date format.</summary>
    public string AustraDate
    {
        get => austraDate;
        set => SetField(ref austraDate, value);
    }

    /// <summary>Gets the root nodes of the variables and definitions tree.</summary>
    public ObservableCollection<ClassNode> Classes
    {
        get => classes;
        set => SetField(ref classes, value);
    }

    public string ErrorText
    {
        get => errorText;
        set => SetField(ref errorText, value);
    }

    public Visibility ShowErrorText
    {
        get => showErrorText;
        set => SetField(ref showErrorText, value);
    }

    private void PrepareWorkspace()
    {
        if (environment != null)
        {
            // Fill the Variables tree.
            List<ClassNode> cList = new();
            allVars.Clear();
            foreach (var g in environment.DataSource.Variables.GroupBy(t => Describe(t.type)))
            {
                ClassNode cNode = new(g.Key);
                foreach ((string? name, Type? type) in g)
                {
                    VarNode? n = CreateVarNode(cNode, name, type!, true);
                    if (n != null)
                        cNode.Nodes.Add(n);
                }
                cList.Add(cNode);
            }
            cList.Sort((x, y) => x.Order.CompareTo(y.Order));
            AllDefinitionsNode defs = new();
            foreach (Definition def in environment.DataSource.AllDefinitions)
            {
                var defNode = new DefinitionNode(defs, def);
                defs.Nodes.Add(defNode);
                allVars[defNode.Name] = defNode;
            }
            cList.Insert(0, defs);
            Classes = new(cList);
            // Select and expand the first node of the tree.
            defs.IsExpanded = true;
            var selectedVar = Classes.Skip(1).FirstOrDefault();
            if (selectedVar != null)
                selectedVar.IsExpanded = true;
            // Show the Formula Editor.
            /*ShowFormulaEditor = Vis.Visible;
            ViewServices.Instance.DoEvents();
            Editor.Focus();
            // Reset serial counters for anonymous expressions.
            serials.Clear();*/
            if (!environment.DataSource.Series.Any())
                AustraDate = "";
            else
            {
                var d = environment.DataSource.Series.Select(s => s.Last().Arg).Max();
                AustraDate = ((DateTime)d).ToString("d@MMMyyyy",
                    System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant();
            }
        }
    }

    public static void DoEvents() =>
        Application.Current.Dispatcher.Invoke(
            DispatcherPriority.Background,
            new Action(() => { }));


    private static string Describe(Type? type) =>
        type == typeof(Series)
        ? "Series"
        : type == typeof(Austra.Library.Matrix) || type == typeof(LMatrix) || type == typeof(RMatrix)
        ? "Matrix"
        : type == typeof(RVector)
        ? "Vector"
        : "Other";

    private VarNode? CreateVarNode(ClassNode cNode, string name, Type type, bool stored)
    {
        VarNode vNode = Environment!.DataSource[name] switch
        {
            Series s => new SeriesNode(cNode, name, s) { Stored = stored },
            Tuple<Series, Series> t => new CompareNode(cNode, name, t),
            FftModel fft => new FftNode(cNode, name, fft),
            ARSModel m => new ARSNode(cNode, name, m),
            ARVModel m => new ARVNode(cNode, name, m),
            /*Matrix m => new MatrixNode(cNode, name, m),
            LMatrix m => new MatrixNode(cNode, name, m),
            RMatrix m => new MatrixNode(cNode, name, m),
            RVector v => new RVectorNode(cNode, name, v),
            CVector cv => new CVectorNode(cNode, name, cv),
            Series<int> s => new CorrNode(cNode, name, s),
            Series<double> s => new PercNode(cNode, name, s),
            EVD evd => new EvdNode(cNode, name, evd),
            Accumulator acc => new AccumNode(cNode, name, acc),
            Tuple<RVector, RVector> t => new CompareVNode(cNode, name, t),
            Tuple<CVector, CVector> t => new CompareCVNode(cNode, name, t),
            MvoModel m => new MvoNode(cNode, name, m),
            LinearSModel lm => new LinearSModelNode(cNode, name, lm),
            LinearVModel lm => new LinearVModelNode(cNode, name, lm),
            DateSpline spline => new DateSplineNode(cNode, name, spline),
            VectorSpline spline => new VectorSplineNode(cNode, name, spline),*/
            _ => new MiscNode(cNode, name, type, Environment.DataSource[name]?.ToString() ?? "")
        };
        allVars[name] = vNode;
        return vNode;
    }

    private static Section? MainSection =>
        ((MainWindow)Application.Current.MainWindow).mainSection;

    private static FlowDocumentScrollViewer Document =>
        ((MainWindow)Application.Current.MainWindow).document;

    public static ICSharpCode.AvalonEdit.TextEditor Editor =>
        ((MainWindow)Application.Current.MainWindow).avalon;

    public static void CloseCompletion() =>
        ((MainWindow)Application.Current.MainWindow).CloseCompletion();

    private ScrollViewer? Scroller
    {
        get
        {
            if (scrollViewer == null)
            {
                // Border is the first child of first child of a ScrolldocumentViewer
                DependencyObject firstChild = VisualTreeHelper.GetChild(Document, 0);
                Decorator? border = VisualTreeHelper.GetChild(firstChild, 0) as Decorator;
                scrollViewer = border?.Child as ScrollViewer;
            }
            return scrollViewer;
        }
    }

    public IList<(string, string)> GetRoots()
    {
        var result = environment!.Engine.GetRoots();
        foreach ((string member, string description) item in environment.Engine.GetRootClasses())
            result.Add(item);
        return result;
    }

    public IList<(string, string)> GetMembers(string text) =>
        environment!.Engine.GetMembers(text);

    public IList<(string, string)> GetClassMembers(string text) =>
        environment!.Engine.GetClassMembers(text);

    public void AppendResult(string variable, string? text)
    {
        if (text != null && text.EndsWith('\n') == false)
            text += '\n';
        MainSection?.ContentEnd.InsertTextInRun($"> {variable}\n{text}\n");
        Scroller?.ScrollToEnd();
    }

    public void AppendControl(string variable, string text, UIElement element)
    {
        MainSection?.ContentEnd.InsertTextInRun($"> {variable}\n{text}");
        MainSection?.Blocks.Add(new BlockUIContainer(element));
        Scroller?.ScrollToEnd();
    }

    public void Evaluate(string text)
    {
        timer.Stop();
        Message = "";
        ShowErrorText = Visibility.Collapsed;
        CloseCompletion();
        if (string.IsNullOrWhiteSpace(text))
            return;
        try
        {
            var (ans, ansType, ansVar) = environment!.Engine.Eval(text);
            ShowTimesMessage(true, true);

            if (ans is Definition def)
            {
                Message = $"Definition {def.Name.ToUpperInvariant()} of type {def.Type.Name} added.";
                AllDefinitionsNode? allDefs = Classes.OfType<AllDefinitionsNode>().FirstOrDefault();
                if (allDefs != null)
                {
                    DefinitionNode newNode = new(allDefs, def);
                    allVars[newNode.Name] = newNode;
                    allDefs.Nodes.Add(newNode);
                    allDefs.IsExpanded = true;
                    newNode.IsSelected = true;
                }
            }
            else if (ans is IList<string> dList)
            {
                foreach (string dn in dList)
                    if (allVars.TryGetValue(dn, out var node))
                    {
                        allVars.Remove(dn);
                        node.Parent!.Nodes.Remove(node);
                    }
                Message = $"Definitions {string.Join(", ", dList)} removed.";
            }
            else if (string.IsNullOrEmpty(ansVar) &&
                allVars.TryGetValue(text.Trim(), out var n) &&
                n is not MiscNode &&
                n is not DefinitionNode)
            {
                n.Show();
                DoEvents();
                Editor.Focus();
            }
            else if (ans != null)
            {
                string form = CleanFormula(text);
                string typeString = ansType?.Name ?? "";
                VarNode? node = ans switch
                {
                    Series s => new SeriesNode(null, typeString, form, s),
                    Tuple<Series, Series> t1 => new CompareNode(null, "Comparison", form, t1),
                    FftModel fft => new FftNode(null, typeString, form, fft),
                    ARSModel m1 => new ARSNode(null, typeString, form, m1),
                    ARVModel m2 => new ARVNode(null, typeString, form, m2),
                    _ => null
                };
                if (node != null)
                    node.Show();
                else if (ans != null)
                {
                    AppendResult(!string.IsNullOrEmpty(ansVar) ? ansVar : text, ans.ToString());
                    return;
                }
            }
        }
        catch (AstException e)
        {
            Editor.CaretOffset = Math.Min(e.Position, Editor.Text.Length);
            ErrorText = e.Message;
            ShowErrorText = Visibility.Visible;
            timer.Start();
        }
        catch (Exception e)
        {
            ErrorText = e.Message;
            ShowErrorText = Visibility.Visible;
            timer.Start();
        }
    }

    private void ShowTimesMessage(bool showComp, bool showExec)
    {
        string msg = "";
        if (showComp && Environment!.Engine.CompileTime is not null)
            msg = "Compile: " +
                Environment.Engine.FormatTime(Environment.Engine.CompileTime.Value);
        if (showExec && Environment!.Engine.ExecutionTime is not null)
            msg += (msg.Length > 0 ? ", execution: " : "Execution: ") +
                Environment!.Engine.FormatTime(Environment.Engine.ExecutionTime.Value);
        if (msg != "")
            Message = msg;
    }

    private static string CleanFormula(string s)
    {
        var m = SetRegex().Match(s);
        return (m.Success ? m.Groups["name"].Value : s).
            Replace("\r\n", " ").
            Replace(" \t", " ").
            Replace("\t", " ");
    }

    static string GetDefaultDataFile() => Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
        @"Austra\data.austra");

    [GeneratedRegex("^\\s*set\\s*(?'name'[\\w]+)\\s*=", RegexOptions.IgnoreCase, "es-ES")]
    private static partial Regex SetRegex();
}
