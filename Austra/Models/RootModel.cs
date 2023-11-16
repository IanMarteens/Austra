using Austra.Parser;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace Austra;

/// <summary>Represents the initial view-model of AUSTRA.</summary>
public sealed partial class RootModel : Entity
{
    /// <summary>Gets the global instance of the root view-model.</summary>
    public static RootModel Instance { get; } = new();

    private static readonly char[] lineChange = ['\r', '\n', ' '];

    /// <summary>Global transient message for the status bar.</summary>
    private string message = "";
    /// <summary>Austra session, containing variables and definitions.</summary>
    private Session? environment;
    /// <summary>The tree of variables, grouped by class. First node are definitions.</summary>
    private ObservableCollection<ClassNode> classes = [];
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
    /// <summary>Gets the visibility of the code editor.</summary>
    private Visibility showFormulaEditor = Visibility.Collapsed;
    private GridLength formulaRowHeight = new(0);
    private int historyIndex = -1;

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
            ErrorText = "";
            timer.Stop();
        };
        CloseAllCommand = new(ExecuteCloseAllCommand, GetHasEnvironment);
        HistoryUpCommand = new(ExecHistoryUp, GetHasEnvironment);
        HistoryDownCommand = new(ExecHistoryDown, GetHasEnvironment);
        EvaluateCommand = new(_ => Evaluate(Editor.Text), GetHasEnvironment);
        CheckTypeCommand = new(_ => CheckType(Editor.Text), GetHasEnvironment);
        ClearCommand = new(_ => MainSection?.Blocks.Clear(), GetHasEnvironment);
    }

    public DelegateCommand CloseAllCommand { get; }

    public DelegateCommand HistoryUpCommand { get; }

    public DelegateCommand HistoryDownCommand { get; }

    public DelegateCommand EvaluateCommand { get; }

    public DelegateCommand CheckTypeCommand { get; }

    public DelegateCommand ClearCommand { get; }

    public DelegateCommand PasteExcelCommand { get; } = new(
        (object? _) =>
        {
            string text = Clipboard.GetText();
            StringBuilder sb = new(text.Length);
            foreach (string line in text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] tokens = line.Replace(',', '.').Split("\t");
                if (sb.Length > 0)
                    sb.AppendLine(";");
                sb.Append(string.Join(", ", tokens));
            }
            if (sb.Length > 0)
                Editor.TextArea.Selection.ReplaceSelectionWithText(
                    "[" + sb.ToString() + "]");
        },
        (object? _) => Clipboard.ContainsText());

    public DelegateCommand FocusEditorCommand { get; } = new((object? _) =>
    {
        Editor.Focus();
        Editor.SelectAll();
    });

    public DelegateCommand OptionsCommand { get; } =
        new(() => new OptionsView().ShowDialog());

    public DelegateCommand AboutCommand { get; } =
        new(() => new AboutView().Show());

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

    public ObservableCollection<string> History { get; } = [];

    public GridLength FormulaRowHeight
    {
        get => formulaRowHeight;
        set => SetField(ref formulaRowHeight, value);
    }

    /// <summary>Gets the visibility of the code editor.</summary>
    public Visibility ShowFormulaEditor
    {
        get => showFormulaEditor;
        set
        {
            if (SetField(ref showFormulaEditor, value))
                FormulaRowHeight = showFormulaEditor == Visibility.Collapsed
                    ? new(0) : new(2, GridUnitType.Star);
        }
    }

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
            List<ClassNode> cList = [];
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
                var d = environment.DataSource.Series.Select(s => s.Last.Arg).Max();
                AustraDate = ((DateTime)d).ToString("d@MMMyyyy",
                    System.Globalization.CultureInfo.InvariantCulture).ToLowerInvariant();
            }
            ShowFormulaEditor = Visibility.Visible;
            MainSection?.ContentEnd.InsertTextInRun("Welcome to AUSTRA!\nv" + Version + "\n\n");
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
        : type == typeof(AMatrix) || type == typeof(LMatrix) || type == typeof(RMatrix)
        ? "Matrices"
        : type == typeof(RVector) || type == typeof(ComplexVector)
        ? "Vectors"
        : type == typeof(ARSModel) || type == typeof(ARVModel)
            || type == typeof(LinearSModel) || type == typeof(LinearVModel)
            || type == typeof(DateSpline) || type == typeof(VectorSpline)
            || type == typeof(MvoModel)
        ? "Models"
        : "Other";

    private VarNode? CreateVarNode(ClassNode cNode, string name, Type type, bool stored)
    {
        if (Environment == null)
            return null;
        object? value = Environment.DataSource[name];
        VarNode vNode = value switch
        {
            Series s => new SeriesNode(cNode, name, s) { Stored = stored },
            Series<double> s => new PercentileNode(cNode, name, s),
            Series<int> s => new CorrelogramNode(cNode, name, s),
            Plot<Series> t => new CompareNode(cNode, name, t),
            Plot<RVector> t => new CompareVNode(cNode, name, t),
            Plot<ComplexVector> t => new CompareCVNode(cNode, name, t),
            FftModel fft => new FftNode(cNode, name, fft),
            ARSModel m => new ARSNode(cNode, name, m),
            ARVModel m => new ARVNode(cNode, name, m),
            Accumulator acc => new AccumNode(cNode, name, acc),
            LinearSModel lm => new LinearSModelNode(cNode, name, lm),
            LinearVModel lm => new LinearVModelNode(cNode, name, lm),
            DateSpline spline => new DateSplineNode(cNode, name, spline),
            VectorSpline spline => new VectorSplineNode(cNode, name, spline),
            AMatrix m => new MatrixNode(cNode, name, m),
            LMatrix m => new MatrixNode(cNode, name, m),
            RMatrix m => new MatrixNode(cNode, name, m),
            RVector v => new VectorNode(cNode, name, v),
            ComplexVector cv => new CVectorNode(cNode, name, cv),
            EVD evd => new EvdNode(cNode, name, evd),
            MvoModel m => new MvoNode(cNode, name, m),
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
                // The border is the first child!
                DependencyObject firstChild = VisualTreeHelper.GetChild(Document, 0);
                Decorator? border = VisualTreeHelper.GetChild(firstChild, 0) as Decorator;
                scrollViewer = border?.Child as ScrollViewer;
            }
            return scrollViewer;
        }
    }

    public IList<Member> GetRoots(int position, string text) =>
        environment!.Engine.GetRoots(position, text);

    public IList<Member> GetMembers(string text) =>
        environment!.Engine.GetMembers(text);

    public IList<Member> GetClassMembers(string text) =>
        environment!.Engine.GetClassMembers(text);

    public void AppendResult(string variable, string? text)
    {
        if (text != null && text.EndsWith('\n') == false)
            text += '\n';
        MainSection?.ContentEnd.InsertTextInRun($"> {variable}\n{text}\n");
        Scroller?.ScrollToEnd();
    }

    public void AppendResult(string variable, Block block, UIElement element)
    {
        MainSection?.ContentEnd.InsertTextInRun($"> {variable}");
        MainSection?.Blocks.Add(block);
        MainSection?.Blocks.Add(new BlockUIContainer(element));
        Scroller?.ScrollToEnd();
    }

    public void AppendControl(string variable, string text, UIElement element)
    {
        MainSection?.ContentEnd.InsertTextInRun($"> {variable}\n{text}");
        MainSection?.Blocks.Add(new BlockUIContainer(element));
        Scroller?.ScrollToEnd();
    }

    public void ExecuteOpenCommand()
    {
        OpenFileDialog dlg = new()
        {
            Filter = "AUSTRA files|*.austra|All files|*.*",
            Title = "Select AUSTRA definitions files",
            FilterIndex = 1,
        };
        if (dlg.ShowDialog(Application.Current.MainWindow) == true)
            LoadFile(dlg.FileName);
    }

    public void LoadFile(string dataFile)
    {
        ExecuteCloseAllCommand();
        Environment = new Session(dataFile);
        PrepareWorkspace();
        DoEvents();
        Editor.Focus();
    }

    private void ExecuteCloseAllCommand()
    {
        Editor.Text = "";
        AustraDate = "";
        allVars.Clear();
        Classes.Clear();
        Environment = null;
        ShowFormulaEditor = Visibility.Collapsed;
        ShowErrorText = Visibility.Collapsed;
        MainSection?.Blocks.Clear();
    }

    public static void AddNodeToEditor(string text)
    {
        if (Editor.SelectionLength == 0)
            if (!string.IsNullOrWhiteSpace(Editor.Text))
                Editor.Text += " " + text;
            else
                Editor.Text = text;
        else
            Editor.SelectedText = text;
    }

    private void CheckType(string text)
    {
        CleanBeforeParsing();
        if (string.IsNullOrWhiteSpace(text))
            return;
        try
        {
            Type result = environment!.Engine.EvalType(text);
            ShowTimesMessage();
            AppendResult(CleanFormula(text),
                result == typeof(Series<double>) ? "Series<double>" :
                result == typeof(Series<int>) ? "Series<int>" :
                result.Name);
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

    public void Evaluate(string text)
    {
        CleanBeforeParsing();
        if (string.IsNullOrWhiteSpace(text))
            return;
        try
        {
            var (ans, ansType, ansVar) = environment!.Engine.Eval(text);
            ShowTimesMessage();

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
            else if (ans is UndefineList list)
            {
                foreach (string dn in list.Definitions)
                    if (allVars.TryGetValue(dn, out var node))
                    {
                        allVars.Remove(dn);
                        node.Parent!.Nodes.Remove(node);
                    }
                Message = $"Definitions {string.Join(", ", list.Definitions)} removed.";
            }
            else
            {
                Editor.SelectAll();
                if (History.Count == 0 || History[^1] != text)
                    History.Add(text);
                historyIndex = -1;
                if (!string.IsNullOrEmpty(ansVar))
                {
                    if (allVars.TryGetValue(ansVar, out VarNode? node) &&
                        node is not DefinitionNode)
                    {
                        node.Parent!.Nodes.Remove(node);
                        if (node.Parent.Nodes.Count == 0)
                            Classes.Remove(node.Parent);
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
                        Series<double> s => new PercentileNode(null, typeString, form, s),
                        Series<int> s => new CorrelogramNode(null, typeString, form, s),
                        Plot<Series> t => new CompareNode(null, "Plot", form, t),
                        Plot<RVector> t => new CompareVNode(null, "Plot", form, t),
                        Plot<ComplexVector> t => new CompareCVNode(null, "Plot", form, t),
                        FftModel fft => new FftNode(null, typeString, form, fft),
                        ARSModel m1 => new ARSNode(null, typeString, form, m1),
                        ARVModel m2 => new ARVNode(null, typeString, form, m2),
                        LinearSModel slm => new LinearSModelNode(null, typeString, form, slm),
                        LinearVModel vlm => new LinearVModelNode(null, typeString, form, vlm),
                        DateSpline dsp => new DateSplineNode(null, typeString, form, dsp),
                        VectorSpline vsp => new VectorSplineNode(null, typeString, form, vsp),
                        Accumulator acc => new AccumNode(null, typeString, form, acc),
                        AMatrix m => new MatrixNode(null, typeString, form, m),
                        LMatrix m => new MatrixNode(null, typeString, form, m),
                        RMatrix m => new MatrixNode(null, typeString, form, m),
                        RVector v => new VectorNode(null, typeString, form, v),
                        ComplexVector v => new CVectorNode(null, typeString, form, v),
                        EVD evd => new EvdNode(null, typeString, form, evd),
                        MvoModel mvo => new MvoNode(null, typeString, form, mvo),
                        _ => null
                    };
                    if (node != null)
                        node.Show();
                    else if (ans != null)
                    {
                        AppendResult(!string.IsNullOrEmpty(ansVar) ? ansVar : form, ans.ToString());
                        return;
                    }
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

    private string savedHistory = "";

    private void ExecHistoryUp()
    {
        if (History.Count == 0)
            return;
        if (historyIndex == -1)
        {
            string oldText = Editor.Text;
            bool modified = oldText != History[^1];
            if (!string.IsNullOrEmpty(oldText) && modified)
                savedHistory = Editor.Text;
            else
                savedHistory = "";
            if (!modified)
            {
                if (History.Count > 1)
                {
                    Editor.Text = History[^2];
                    historyIndex = History.Count - 2;
                }
                else
                    return;
            }
            else
            {
                Editor.Text = History[^1];
                historyIndex = History.Count - 1;
            }
        }
        else if (historyIndex > 0)
            Editor.Text = History[--historyIndex];
        else if (savedHistory != "")
        {
            historyIndex = -1;
            Editor.Text = savedHistory;
        }
        else
        {
            Editor.Text = History[^1];
            historyIndex = History.Count - 1;
        }
        Editor.CaretOffset = Editor.Text.Length;
    }

    private void ExecHistoryDown()
    {
        if (historyIndex == -1)
        {
            if (History.Count == 0)
                return;
            string oldText = Editor.Text;
            bool modified = oldText != History[^1];
            if (!string.IsNullOrEmpty(oldText) && modified)
                savedHistory = Editor.Text;
            else
                savedHistory = "";
            Editor.Text = History[0];
            historyIndex = 0;
        }
        else if (historyIndex < History.Count - 1)
            Editor.Text = History[++historyIndex];
        else if (savedHistory != "")
        {
            historyIndex = -1;
            Editor.Text = savedHistory;
        }
        else
        {
            Editor.Text = History[0];
            historyIndex = 0;
        }
        Editor.CaretOffset = Editor.Text.Length;
    }

    private void CleanBeforeParsing()
    {
        timer.Stop();
        Message = "";
        ErrorText = "";
        ShowErrorText = Visibility.Collapsed;
        CloseCompletion();
    }

    private static string CleanFormula(string s)
    {
        var m = SetRegex().Match(s);
        return (m.Success ? m.Groups["name"].Value : s).
            Replace("\r\n", " ").
            Replace(" \t", " ").
            Replace("\t", " ").TrimEnd(lineChange);
    }

    /// <summary>Shows the compiling and execution time in the status bar.</summary>
    private void ShowTimesMessage()
    {
        string msg = "";
        IAustraEngine engine = Environment!.Engine;
        Properties.Settings props = Properties.Settings.Default;
        if (props.ShowCompileTime)
            if (engine.CompileTime is not null)
                if (engine.GenerationTime is not null)
                    msg = "Compile: " + engine.FormatTime(engine.CompileTime.Value) + 
                        ", code generation: " + engine.FormatTime(engine.GenerationTime.Value);
                else
                    msg = "Compile: " + engine.FormatTime(engine.CompileTime.Value);
        if (props.ShowExecutionTime && engine.ExecutionTime is not null)
            msg += (msg.Length > 0 ? ", execution: " : "Execution: ") +
                engine.FormatTime(engine.ExecutionTime.Value);
        if (msg != "")
            Message = msg;
    }

    /// <summary>Gets a regex that matches a set statement</summary>
    [GeneratedRegex("^\\s*set\\s*(?'name'[\\w]+)\\s*=", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex SetRegex();
}
