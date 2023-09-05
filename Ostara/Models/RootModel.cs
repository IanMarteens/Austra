using Austra.Parser;
using System.IO;
using System.Windows.Documents;
using System.Windows.Media;

namespace Ostara;

/// <summary>Represents the initial view-model of Ostara.</summary>
public sealed partial class RootModel : Entity
{
    public static RootModel Instance { get; } = new();

    private Session? environment;
    private ObservableCollection<ClassNode> classes = new();
    /// <summary>Maps names into variable nodes.</summary>
    private readonly Dictionary<string, VarNode> allVars = new(StringComparer.OrdinalIgnoreCase);
    private string austraDate = "";
    private ScrollViewer? scrollViewer;

    public RootModel()
    {
        CommonMatrix.TERMINAL_COLUMNS = 160;
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

    public Session? Environment
    {
        get => environment;
        set => SetField(ref environment, value, nameof(HasEnvironment));
    }

    public bool HasEnvironment => Environment != null;

    public bool GetHasEnvironment() => Environment != null;

    public string AustraDate
    {
        get => austraDate;
        set => SetField(ref austraDate, value);
    }

    public ObservableCollection<ClassNode> Classes
    {
        get => classes;
        set => SetField(ref classes, value);
    }

    private void PrepareWorkspace()
    {
        if (environment != null)
        {
            // Fill the Variables tree.
            var cList = new List<ClassNode>();
            allVars.Clear();
            foreach (var g in environment.DataSource.Variables.GroupBy(t => Describe(t.type)))
            {
                var cNode = new ClassNode(g.Key);
                foreach ((string? name, Type? type) in g)
                {
                    var n = CreateVarNode(cNode, name, type!, true);
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
                AustraDate = ((DateTime)d).ToString("d@MMMyyyy");
            }
        }
    }

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
            /*Matrix m => new MatrixNode(cNode, name, m),
            LMatrix m => new MatrixNode(cNode, name, m),
            RMatrix m => new MatrixNode(cNode, name, m),
            RVector v => new RVectorNode(cNode, name, v),
            CVector cv => new CVectorNode(cNode, name, cv),
            FftModel fft => new FftNode(cNode, name, fft),
            Series<int> s => new CorrNode(cNode, name, s),
            Series<double> s => new PercNode(cNode, name, s),
            EVD evd => new EvdNode(cNode, name, evd),
            Accumulator acc => new AccumNode(cNode, name, acc),
            Tuple<Series, Series> t => new CompareNode(cNode, name, t),
            Tuple<RVector, RVector> t => new CompareVNode(cNode, name, t),
            Tuple<CVector, CVector> t => new CompareCVNode(cNode, name, t),
            MvoModel m => new MvoNode(cNode, name, m),
            LinearSModel lm => new LinearSModelNode(cNode, name, lm),
            LinearVModel lm => new LinearVModelNode(cNode, name, lm),
            ARSModel m => new ARSNode(cNode, name, m),
            ARVModel m => new ARVNode(cNode, name, m),
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
        foreach (var item in environment.Engine.GetRootClasses())
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

    public void Evaluate(string text)
    {
        try
        {
            var (ans, ansType, ansVar) = environment!.Engine.Eval(text);
            if (ans != null)
                AppendResult(!string.IsNullOrEmpty(ansVar) ? ansVar : text, ans.ToString());
        }
        catch (AstException e)
        {
            Editor.CaretOffset = e.Position;
            MainSection?.ContentEnd.InsertTextInRun(e.Message);
        }
        catch (Exception e)
        {
            MainSection?.ContentEnd.InsertTextInRun(e.Message);
        }
    }

    static string GetDefaultDataFile() => Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments),
        @"Austra\data.austra");
}
