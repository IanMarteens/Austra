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
    /// <summary>Height for the formula editor row. 0 means collapsed.</summary>
    private GridLength formulaRowHeight = new(0);
    /// <summary>
    /// Current position in the formula history. -1 means no history, 0 is the last
    /// </summary>
    private int historyIndex = -1;
    private string savedHistory = "";

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
        CloseAllCommand = new(ExecuteCloseAllCommand, GetHasEnvironment);
        HistoryUpCommand = new(ExecHistoryUp, GetHasEnvironment);
        HistoryDownCommand = new(ExecHistoryDown, GetHasEnvironment);
        EvaluateCommand = new(_ => Evaluate(Editor.Text), GetHasEnvironment);
        CheckTypeCommand = new(_ => CheckType(Editor.Text), GetHasEnvironment);
        ClearCommand = new(_ => MainSection?.Blocks.Clear(), GetHasEnvironment);
        DebugFormula = new(ExecuteDebugFormula, GetHasEnvironment);
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

    public DelegateCommand DebugFormula { get; }

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
        set
        {
            if (SetField(ref showErrorText, value))
                OnPropertyChanged(nameof(ErrorTextHeight), nameof(ErrorIconSize));
        }
    }

    public int ErrorTextHeight => showErrorText == Visibility.Collapsed ? 0 : 18;

    public int ErrorIconSize => showErrorText == Visibility.Collapsed ? 0 : 15;

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
            environment.Engine.DebugFormulas = Properties.Settings.Default.DebugFormulas;
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
        : type?.IsAssignableTo(typeof(IVector)) == true
        ? "Vectors"
        : type == typeof(ARSModel) || type == typeof(ARVModel)
            || type == typeof(LinearSModel) || type == typeof(LinearVModel)
            || type == typeof(DateSpline) || type == typeof(VectorSpline)
            || type == typeof(MvoModel)
        ? "Models"
        : type?.IsAssignableTo(typeof(Sequence<,>)) == true
        ? "Sequences"
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
            Plot<Series> p => new PlotSNode(cNode, name, p),
            Plot<CVector> p => new PlotCVNode(cNode, name, p),
            Plot<DVector> p => new PlotDVNode(cNode, name, p),
            Plot<NVector> p => new PlotNVNode(cNode, name, p),
            FftModel fft => new FftNode(cNode, name, fft),
            ARSModel m => new ARSNode(cNode, name, m),
            ARVModel m => new ARVNode(cNode, name, m),
            MASModel m => new MASNode(cNode, name, m),
            MAVModel m => new MAVNode(cNode, name, m),
            Accumulator acc => new AccumNode(cNode, name, acc),
            LinearSModel lm => new LinearSModelNode(cNode, name, lm),
            LinearVModel lm => new LinearVModelNode(cNode, name, lm),
            DateSpline spline => new DateSplineNode(cNode, name, spline),
            VectorSpline spline => new VectorSplineNode(cNode, name, spline),
            AMatrix m => new MatrixNode(cNode, name, m),
            LMatrix m => new MatrixNode(cNode, name, m),
            RMatrix m => new MatrixNode(cNode, name, m),
            DVector v => new VectorNode(cNode, name, v),
            CVector cv => new CVectorNode(cNode, name, cv),
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
            Type[] result = environment!.Engine.EvalType(text);
            ShowTimesMessage();
            AppendResult(
                CleanFormula(text),
                string.Join(", ", result.Select(static t =>
                    t == typeof(Series<double>) ? "Series<double>" :
                    t == typeof(Series<int>) ? "Series<int>" :
                    t.IsGenericType ? GetFriendlyName(t) :
                    t.Name)));
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

        static string GetFriendlyName(Type t)
        {
            string result = t.Name;
            int i = result.IndexOf('`');
            if (i >= 0)
                result = result[..i] + "<" +
                    string.Join(", ", t.GetGenericArguments().Select(GetFriendlyName)) + ">";
            return result;
        }
    }

    public void Evaluate(string text)
    {
        CleanBeforeParsing();
        if (string.IsNullOrWhiteSpace(text))
            return;
        try
        {
            IAustraEngine engine = environment!.Engine;
            engine.Eval(text);
            ShowTimesMessage();
            bool firstAnswer = true;
            for (Queue<AustraAnswer> queue = engine.AnswerQueue;
                queue.TryDequeue(out AustraAnswer answer);)
                if (answer.Value is Definition def)
                {
                    AllDefinitionsNode? allDefs = Classes.OfType<AllDefinitionsNode>().FirstOrDefault();
                    if (allDefs != null)
                    {
                        DefinitionNode newNode = new(allDefs, def);
                        allVars[newNode.Name] = newNode;
                        allDefs.Nodes.Add(newNode);
                        allDefs.IsExpanded = true;
                        newNode.IsSelected = true;
                    }
                    Message = $"Definition {def.Name.ToUpperInvariant()} of type {def.Type.Name} added.";
                }
                else if (answer.Value is UndefineList list)
                {
                    foreach (string dn in list.Definitions)
                        if (allVars.TryGetValue(dn, out VarNode? oldNode))
                        {
                            allVars.Remove(dn);
                            oldNode.Parent!.Nodes.Remove(oldNode);
                        }
                    Message = $"Definitions {string.Join(", ", list.Definitions)} removed.";
                }
                else if (!string.IsNullOrEmpty(answer.Variable))
                {
                    if (allVars.TryGetValue(answer.Variable, out VarNode? node) &&
                        node is not DefinitionNode)
                    {
                        node.Parent!.Nodes.Remove(node);
                        if (node.Parent.Nodes.Count == 0)
                            Classes.Remove(node.Parent);
                        allVars.Remove(node.Name);
                    }
                    if (answer.Type != null)
                    {
                        string typeName = Describe(answer.Type);
                        ClassNode? parent = Classes.FirstOrDefault(c => c.Name == typeName);
                        if (parent == null)
                        {
                            parent = new(typeName);
                            Classes.Add(parent);
                        }
                        VarNode? varNode = CreateVarNode(parent, answer.Variable, answer.Type, false);
                        if (varNode != null)
                        {
                            parent.Nodes.Add(varNode);
                            parent.IsExpanded = true;
                            varNode.IsSelected = true;
                        }
                    }
                }
                else
                {
                    if (firstAnswer)
                    {
                        Editor.SelectAll();
                        if (History.Count == 0 || History[^1] != text)
                            History.Add(text);
                        historyIndex = -1;
                        firstAnswer = false;
                    }
                    if (answer.Value != null)
                    {
                        string form = CleanFormula(engine.RangeQueue.TryDequeue(out Range range)
                            ? text[range] : text);
                        VarNode? node = answer.Value switch
                        {
                            Series s => new SeriesNode(form, s),
                            Series<double> s => new PercentileNode(form, s),
                            Series<int> s => new CorrelogramNode(form, s),
                            FftModel fft => new FftNode(form, fft),
                            Plot<CVector> p => new PlotCVNode(form, p),
                            Plot<DVector> p => new PlotDVNode(form, p),
                            Plot<NVector> p => new PlotNVNode(form, p),
                            Plot<Series> p => new PlotSNode(form, p),
                            ARSModel m1 => new ARSNode(form, m1),
                            ARVModel m2 => new ARVNode(form, m2),
                            MASModel m1 => new MASNode(form, m1),
                            MAVModel m2 => new MAVNode(form, m2),
                            LinearSModel slm => new LinearSModelNode(form, slm),
                            LinearVModel vlm => new LinearVModelNode(form, vlm),
                            DateSpline dsp => new DateSplineNode(form, dsp),
                            VectorSpline vsp => new VectorSplineNode(form, vsp),
                            Accumulator acc => new AccumNode(form, acc),
                            AMatrix m => new MatrixNode(form, m),
                            LMatrix m => new MatrixNode(form, m),
                            RMatrix m => new MatrixNode(form, m),
                            DVector v => new VectorNode(form, v),
                            CVector v => new CVectorNode(form, v),
                            EVD evd => new EvdNode(form, evd),
                            MvoModel mvo => new MvoNode(form, mvo),
                            _ => null
                        };
                        if (node != null)
                            node.Show();
                        else
                            AppendResult(form, answer.Value.ToString());
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

    private void ExecuteDebugFormula(object? _)
    {
        if (environment?.Engine.DebugFormulas == true)
            Message = environment.Engine.LastFormula;
    }

    private void CleanBeforeParsing()
    {
        timer.Stop();
        Message = "";
        ErrorText = "";
        ShowErrorText = Visibility.Collapsed;
        CloseCompletion();
    }

    private static string CleanFormula(string s) =>
        string.Join(" ",
            s.Split(System.Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries).Select(RemoveComment));

    private static string RemoveComment(string line)
    {
        line = line.TrimStart();
        int idx = line.IndexOf("--");
        return idx < 0 ? line : line[..idx].TrimEnd();
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
