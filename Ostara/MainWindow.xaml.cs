using ICSharpCode.AvalonEdit.CodeCompletion;

namespace Ostara;

/// <summary>Interaction logic for MainWindow.xaml</summary>
public partial class MainWindow : Window
{
    private CompletionWindow? completionWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = RootModel.Instance;
        avalon.SyntaxHighlighting = ResLoader.LoadHighlightingDefinition("austra.xshd");
        avalon.TextArea.TextEntered += TextArea_TextEntered;
        avalon.TextArea.TextEntering += TextArea_TextEntering;
        mainSection.ContentEnd.InsertTextInRun("Welcome to Ostara!\nv" + typeof(Series).Assembly.GetName().Version!.ToString(3) + "\n\n");
    }

    private RootModel Root => (RootModel)DataContext;

    public void CloseCompletion() => completionWindow?.Close();

    private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0)
            if (completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Whenever a non-letter is typed while the completion window is open,
                    // insert the currently selected element.
                    completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            else if (char.IsLetter(e.Text[0]))
                if (avalon.CaretOffset == 0)
                    ShowCodeCompletion(RootModel.Instance.GetRoots());
                else if (avalon.Document.GetCharAt(avalon.CaretOffset - 1) == '.' &&
                    avalon.CaretOffset > 1)
                {
                    string frag = avalon.Document.GetText(0, avalon.CaretOffset - 1);
                    ShowCodeCompletion(RootModel.Instance.GetMembers(frag));
                }
    }

    private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        IList<(string, string)> list;
        if (e.Text == ".")
            list = RootModel.Instance.GetMembers(GetFragment());
        else if (e.Text == ":")
            list = RootModel.Instance.GetClassMembers(GetFragment());
        else 
            list = new List<(string, string)>();
        ShowCodeCompletion(list);

        string GetFragment() => avalon.Document.GetText(0, avalon.CaretOffset - 1);
    }

    private void ShowCodeCompletion(IList<(string, string)> list)
    {
        if (list?.Count > 0)
        {
            completionWindow = new CompletionWindow(avalon.TextArea);
            var data = completionWindow.CompletionList.CompletionData;
            foreach (var (member, description) in list)
                data.Add(new CompletionData(member, description));
            completionWindow.Show();
            completionWindow.Closed += CompletionListClosed;
        }
    }

    private void CompletionListClosed(object? sender, EventArgs e) => completionWindow = null;

    private void CloseCmdExecuted(object sender, ExecutedRoutedEventArgs e) => Close();

    private void PlayCmdExecuted(object sender, ExecutedRoutedEventArgs e) =>
        Root.Evaluate(avalon.Text);

    private void AboutClick(object sender, RoutedEventArgs e) =>
        new AboutView() { Owner = this }.ShowDialog();
}
