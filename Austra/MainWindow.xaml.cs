using Austra.Parser;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System.IO;

namespace Austra;

/// <summary>Interaction logic for MainWindow.xaml</summary>
public partial class MainWindow : Window
{
    private CompletionWindow? completionWindow;
    private OverloadInsightWindow? insightWindow;
    private bool gMode, qMode;

    /// <summary>
    /// Initializes a new instance of the MainWindow class and sets up the main application window,
    /// event handlers, and data context.
    /// </summary>
    /// <remarks>This constructor configures the main window's data context, syntax highlighting,
    /// and event subscriptions required for user interaction and document editing.
    /// </remarks>
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += MainWindowStateChangeRaised;
        DataContext = RootModel.Instance;
        avalon.SyntaxHighlighting = ResLoader.LoadHighlightingDefinition("austra.xshd");
        avalon.TextArea.TextEntered += TextArea_TextEntered;
        avalon.TextArea.TextEntering += TextArea_TextEntering;
        avalon.PreviewKeyDown += AvalonPreviewKeyDown;
        avalon.Document.Changed += (s, e) => DocumentChanged(e);
        Loaded += MainWindowLoaded;
    }

    /// <summary>
    /// Handles the Loaded event of the main window and attempts to automatically load a data file
    /// if the autoload setting is enabled.
    /// </summary>
    /// <remarks>If the autoload setting is enabled, this method attempts to locate and load the
    /// specified data file. If the file path is not set or the file does not exist in the expected
    /// location, it searches common directories such as the user's Documents folder.
    /// No action is taken if the file cannot be found.</remarks>
    /// <param name="sender">The source of the event, typically the main window instance.</param>
    /// <param name="e">The event data associated with the Loaded event.</param>
    private void MainWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (Properties.Settings.Default.Autoload)
        {
            string file = Properties.Settings.Default.AutoloadFile;
            if (string.IsNullOrWhiteSpace(file))
                file = @"Austra\data.austra";
            if (!File.Exists(file))
            {
                string docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string dataFile = Path.Combine(docs, Path.Combine("Austra", file));
                file = File.Exists(dataFile)
                    ? dataFile
                    : Path.Combine(docs, file);
            }
            if (File.Exists(file))
                Root.LoadFile(file);
        }
    }

    private void MainWindowStateChangeRaised(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            MainWindowBorder.BorderThickness = new Thickness(8);
            RestoreButton.Visibility = Visibility.Visible;
            MaximizeButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            MainWindowBorder.BorderThickness = new Thickness(0);
            RestoreButton.Visibility = Visibility.Collapsed;
            MaximizeButton.Visibility = Visibility.Visible;
        }
    }

    /// <summary>
    /// Casts the data context as a <see cref="RootModel"/> instance.
    /// </summary>
    private RootModel Root => (RootModel)DataContext;

    /// <summary>Closes the completion and insight windows, if they are open.</summary>
    public void CloseCompletion()
    {
        completionWindow?.Close();
        insightWindow?.Close();
    }

    private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0)
            if (completionWindow != null)
            {
                string? text = completionWindow.CompletionList.SelectedItem?.Text;
                if (e.Text[0] == '(' && text?.EndsWith("::") == true)
                    completionWindow.CompletionList.SelectItem(text[..^2] + "(");
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Avoid inserting the same text twice!
                    if (completionWindow.CompletionList.SelectedItem?.Text.EndsWith(e.Text) == true)
                        e.Handled = true;
                    string prefix = avalon.Document.GetText(
                        completionWindow.StartOffset, avalon.CaretOffset - completionWindow.StartOffset);
                    if (prefix.All(c => char.IsDigit(c) || c == '.'))
                        completionWindow.Close();
                    else
                        completionWindow.CompletionList.RequestInsertion(e);
                }
            }
            else if (char.IsLetter(e.Text[0]))
            {
                int offset = avalon.CaretOffset;
                if (offset == 0)
                    ShowCodeCompletion(RootModel.Instance.GetRoots(0, ""));
                else if (offset > 0)
                {
                    string fragment = GetFragment(0);
                    if (IsIdentifier(fragment))
                    {
                        ShowCodeCompletion(RootModel.Instance.GetRoots(offset, avalon.Text));
                        completionWindow!.StartOffset = offset - fragment.Length;
                    }
                    else
                    {
                        fragment = fragment.TrimEnd();
                        if (fragment.EndsWith('.'))
                            ShowCodeCompletion(RootModel.Instance.GetMembers(fragment));
                        else if (fragment.EndsWith('('))
                        {
                            if (fragment.Length > 2 && char.IsLetterOrDigit(fragment[^2]))
                                ShowCodeCompletion(RootModel.Instance.GetRoots(offset, avalon.Text));
                        }
                        else if (fragment.EndsWith(','))
                            ShowCodeCompletion(RootModel.Instance.GetRoots(offset, avalon.Text));
                        else if (fragment.EndsWith("::"))
                            ShowCodeCompletion(RootModel.Instance.GetClassMembers(fragment));
                    }
                }
            }

        static bool IsIdentifier(string s) => s.Length > 0 &&
            char.IsLetter(s[0]) && s.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private void TextArea_TextEntered(object sender, TextCompositionEventArgs e)
    {
        if (e.Text == "." && avalon.CaretOffset > 1 && avalon.Text[avalon.CaretOffset - 2] == '.')
            return;
        ShowCodeCompletion(e.Text == "."
            ? RootModel.Instance.GetMembers(GetFragment())
            : e.Text == ":"
            ? RootModel.Instance.GetClassMembers(GetFragment())
            : e.Text == "("
            ? RootModel.Instance.GetRoots(avalon.CaretOffset, avalon.Text)
            : null);
    }

    private string GetFragment(int delta = 1) =>
        avalon.Document.GetText(0, avalon.CaretOffset - delta);

    private void ShowCodeCompletion(IList<Member>? list)
    {
        if (list?.Count > 0)
            try
            {
                completionWindow = new CompletionWindow(avalon.TextArea);
                IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
                foreach ((string member, string description) in list)
                    data.Add(new CompletionData(member, description));
                completionWindow.Show();
                if (insightWindow != null)
                    completionWindow.Top += insightWindow.ActualHeight;
                completionWindow.Closed += CompletionListClosed;
            }
            catch { }
    }

    /// <summary>
    /// Displays code insight information for the current code fragment, based on the position of
    /// the nearest opening parenthesis.
    /// </summary>
    /// <remarks>This method analyzes the current code fragment to determine the context for displaying code
    /// insight, such as parameter information. If no opening parenthesis is found in the fragment,
    /// no code insight is shown.</remarks>
    public void ShowCodeInsight()
    {
        string text = GetFragment(0);
        int i = text.Length - 1;
        while (i >= 0 && text[i] != '(')
            i--;
        if (i < 0)
            return;
        ShowCodeInsight(text[..i].TrimEnd());
    }

    private void ShowCodeInsight(string fragment)
    {
        var (header, parameters) = RootModel.Instance.GetParameterInfo(fragment);
        if (parameters?.Count > 0)
        {
            insightWindow = new(avalon.TextArea)
            {
                Provider = new InsightProvider(header, parameters)
            };
            insightWindow.Show();
            insightWindow.Closed += InsightWindowClosed;
            if (completionWindow is null)
                ShowCodeCompletion(
                    RootModel.Instance.GetRoots(avalon.CaretOffset, avalon.Text));
            else
                completionWindow.Top += insightWindow.ActualHeight;
        }
    }

    /// <summary>Intercepts changes caused by CodeCompletion and CodeInsight.</summary>
    private void DocumentChanged(DocumentChangeEventArgs e)
    {
        if (e.InsertedText.Text.EndsWith(')'))
            insightWindow?.Close();
        else if (e.InsertedText.Text.EndsWith('(')
            || e.InsertedText.Text.EndsWith("(x => "))
            ShowCodeInsight(GetFragment(
                e.InsertedText.Text.EndsWith('(') ? 1 : "(x => ".Length));
        else if (e.InsertedText.Text.EndsWith("::"))
            ShowCodeCompletion(
                RootModel.Instance.GetClassMembers(GetFragment()));
    }

    /// <summary>
    /// Nullifies the reference to the completion window when it is closed, so that it can be recreated later.
    /// </summary>
    private void CompletionListClosed(object? sender, EventArgs e) => completionWindow = null;

    /// <summary>
    /// Nullifies the reference to the insight window when it is closed, so that it can be recreated later.
    /// </summary>
    private void InsightWindowClosed(object? sender, EventArgs e) => insightWindow = null;

    /// <summary>
    /// Closes the window when the close command is executed,
    /// which is triggered by the close button in the title bar and by pressing Alt+F4.
    /// </summary>
    private void CloseCmdExecuted(object sender, ExecutedRoutedEventArgs e) => Close();

    private void ExecuteOpen(object sender, ExecutedRoutedEventArgs e) =>
        Root.ExecuteOpenCommand();

    private void ExecuteMinimize(object sender, ExecutedRoutedEventArgs e) =>
        SystemCommands.MinimizeWindow(this);

    // Maximize
    private void ExecuteMaximize(object sender, ExecutedRoutedEventArgs e) =>
        SystemCommands.MaximizeWindow(this);

    // Restore
    private void ExecuteRestore(object sender, ExecutedRoutedEventArgs e) =>
        SystemCommands.RestoreWindow(this);

    private void ExecuteClose(object sender, ExecutedRoutedEventArgs e) =>
        SystemCommands.CloseWindow(this);

    /// <summary>
    /// Transforms key combinations into either Greek leters or symbols.
    /// </summary>
    private void AvalonPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.G && Keyboard.Modifiers == ModifierKeys.Control)
        {
            gMode = !gMode;
            e.Handled = true;
        }
        else if (e.Key == Key.Q && Keyboard.Modifiers == ModifierKeys.Control)
        {
            qMode = !qMode;
            e.Handled = true;
        }
        // Avoid interfering with the normal behavior of the Shift keys.
        else if (e.Key != Key.LeftShift & e.Key != Key.RightShift)
            if (gMode)
            {
                if (GreekSymbols.TryTransform(e.Key, out char ch))
                {
                    avalon.SelectedText = "";
                    avalon.Document.Insert(avalon.CaretOffset, ch.ToString());
                    e.Handled = true;
                }
                gMode = false;
            }
            else if (qMode)
            {
                if (GreekSymbols.TryTransformSymbol(e.Key, out char ch))
                {
                    avalon.SelectedText = "";
                    avalon.Document.Insert(avalon.CaretOffset, ch.ToString());
                    e.Handled = true;
                }
                qMode = false;
            }
    }
}
