using Austra.Parser;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Ostara;

/// <summary>Represents the initial view-model of Ostara.</summary>
public sealed partial class RootModel : Entity
{
    /// <summary>Gets the global instance of the root view-model.</summary>
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
    /// <summary>Either a compiling or a runtime error.</summary>
    private string errorText = "";
    /// <summary>Is there an error text to show?</summary>
    private Visibility showErrorText = Visibility.Collapsed;

    /// <summary>Creates a new instance of the root view-model.</summary>
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

    /// <summary>Gets either a compiling or a runtime error.</summary>
    public string ErrorText
    {
        get => errorText;
        set => SetField(ref errorText, value);
    }

    /// <summary>Is there an error text to show?</summary>
    public Visibility ShowErrorText
    {
        get => showErrorText;
        set => SetField(ref showErrorText, value);
    }

    /// <summary>
    /// Prepares the workspace, filling the variables tree and selecting the first node.
    /// </summary>
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

    /// <summary>Allows the event loop to run once.</summary>
    public static void DoEvents() =>
        Application.Current.Dispatcher.Invoke(
            DispatcherPriority.Background,
            new Action(() => { }));


    /// <summary>Describes a data type in a human-readable way.</summary>
    private static string Describe(Type? type) =>
        type == typeof(Series)
        ? "Series"
        : type == typeof(Austra.Library.Matrix) || type == typeof(LMatrix) || type == typeof(RMatrix)
        ? "Matrix"
        : type == typeof(RVector) || type == typeof(ComplexVector)
        ? "Vector"
        : "Other";

    private VarNode? CreateVarNode(ClassNode cNode, string name, Type type, bool stored)
    {
        if (Environment == null)
            return null;
        object? value = Environment.DataSource[name];
        VarNode vNode = value switch
        {
            Series s => new SeriesNode(cNode, name, s) { Stored = stored },
            Tuple<Series, Series> t => new CompareNode(cNode, name, t),
            FftModel fft => new FftNode(cNode, name, fft),
            ARSModel m => new ARSNode(cNode, name, m),
            ARVModel m => new ARVNode(cNode, name, m),
            Accumulator acc => new AccumNode(cNode, name, acc),
            LinearSModel lm => new LinearSModelNode(cNode, name, lm),
            LinearVModel lm => new LinearVModelNode(cNode, name, lm),
            DateSpline spline => new DateSplineNode(cNode, name, spline),
            VectorSpline spline => new VectorSplineNode(cNode, name, spline),
            Austra.Library.Matrix m => new MatrixNode(cNode, name, m),
            LMatrix m => new MatrixNode(cNode, name, m),
            RMatrix m => new MatrixNode(cNode, name, m),
            RVector v => new VectorNode(cNode, name, v),
            ComplexVector cv => new CVectorNode(cNode, name, cv),
            EVD evd => new EvdNode(cNode, name, evd),
            /*Series<int> s => new CorrNode(cNode, name, s),
            Series<double> s => new PercNode(cNode, name, s),
            Tuple<RVector, RVector> t => new CompareVNode(cNode, name, t),
            Tuple<CVector, CVector> t => new CompareCVNode(cNode, name, t),
            MvoModel m => new MvoNode(cNode, name, m),*/
            _ => new MiscNode(cNode, name, type, value?.ToString() ?? "")
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

    public IList<(string, string)> GetRoots() =>
        environment!.Engine.GetRoots();

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
            else if (!string.IsNullOrEmpty(ansVar))
            {
                if (allVars.TryGetValue(ansVar, out VarNode? node) &&
                    node is not DefinitionNode)
                {
                    node.Parent!.Nodes.Remove(node);
                    allVars.Remove(node.Name);
                }
                if (ansType == null)
                {
                    ans = Environment!.Engine.Source[ansVar];
                    ansType = ans?.GetType();
                }
                if (ansType != null)
                {
                    string typeName = Describe(ansType);
                    ClassNode? parent = Classes.FirstOrDefault(c => c.Name == typeName);
                    if (parent == null)
                    {
                        parent = new ClassNode(typeName);
                        Classes.Add(parent);
                    }
                    node = CreateVarNode(parent, ansVar, ansType, false);
                    if (node != null)
                    {
                        parent.Nodes.Add(node);
                        parent.IsExpanded = true;
                        node.IsSelected = true;
                    }
                }
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
                    LinearSModel slm => new LinearSModelNode(null, typeString, form, slm),
                    LinearVModel vlm => new LinearVModelNode(null, typeString, form, vlm),
                    DateSpline dsp => new DateSplineNode(null, typeString, form, dsp),
                    VectorSpline vsp => new VectorSplineNode(null, typeString, form, vsp),
                    Accumulator acc => new AccumNode(null, typeString, form, acc),
                    Austra.Library.Matrix m => new MatrixNode(null, typeString, form, m),
                    LMatrix m => new MatrixNode(null, typeString, form, m),
                    RMatrix m => new MatrixNode(null, typeString, form, m),
                    RVector v => new VectorNode(null, typeString, form, v),
                    ComplexVector v => new CVectorNode(null, typeString, form, v),
                    EVD evd => new EvdNode(null, typeString, form, evd),
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
        
        static string CleanFormula(string s)
        {
            var m = SetRegex().Match(s);
            return (m.Success ? m.Groups["name"].Value : s).
                Replace("\r\n", " ").
                Replace(" \t", " ").
                Replace("\t", " ");
        }
    }

    /// <summary>Shows the compiling and execution time in the status bar.</summary>
    /// <param name="showComp">Must we show the compiling time?</param>
    /// <param name="showExec">Must we show the execution time?</param>
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

    static string GetDefaultDataFile() => Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
        @"Austra\data.austra");

    /// <summary>Gets a regex that matches a set statement</summary>
    [GeneratedRegex("^\\s*set\\s*(?'name'[\\w]+)\\s*=", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SetRegex();
}
