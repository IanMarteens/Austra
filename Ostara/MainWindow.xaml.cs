using Austra.Parser;
using ICSharpCode.AvalonEdit.CodeCompletion;

namespace Ostara;

/// <summary>Interaction logic for MainWindow.xaml</summary>
public partial class MainWindow : Window
{
    private static readonly Dictionary<Key, char> tmgLo = new()
    {
        [Key.A] = 'α',
        [Key.B] = 'β',
        [Key.C] = 'ψ',
        [Key.D] = 'δ',
        [Key.E] = 'ε',
        [Key.F] = 'φ',
        [Key.G] = 'γ',
        [Key.H] = 'η',
        [Key.J] = 'ξ',
        [Key.L] = 'λ',
        [Key.M] = 'μ',
        [Key.N] = 'ν',
        [Key.O] = 'ω',
        [Key.P] = 'π',
        [Key.R] = 'ρ',
        [Key.S] = 'σ',
        [Key.T] = 'τ',
        [Key.U] = 'Θ',
        [Key.Z] = 'ζ',
    };
    private static readonly Dictionary<Key, char> tmgUp = new()
    {
        [Key.D] = 'Δ',
        [Key.E] = 'ε',
        [Key.G] = 'Γ',
        [Key.J] = 'Ξ',
        [Key.L] = 'Λ',
        [Key.O] = 'Ω',
        [Key.P] = 'Π',
        [Key.S] = 'Σ',
        [Key.U] = 'θ',
    };

    private CompletionWindow? completionWindow;
    private bool gMode;

    public MainWindow()
    {
        InitializeComponent();
        StateChanged += MainWindowStateChangeRaised;
        DataContext = RootModel.Instance;
        avalon.SyntaxHighlighting = ResLoader.LoadHighlightingDefinition("austra.xshd");
        avalon.TextArea.TextEntered += TextArea_TextEntered;
        avalon.TextArea.TextEntering += TextArea_TextEntering;
        avalon.PreviewKeyDown += AvalonPreviewKeyDown;
        mainSection.ContentEnd.InsertTextInRun("Welcome to Ostara!\nv" + RootModel.Version + "\n\n");
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

    private RootModel Root => (RootModel)DataContext;

    public void CloseCompletion() => completionWindow?.Close();

    private void TextArea_TextEntering(object sender, TextCompositionEventArgs e)
    {
        if (e.Text.Length > 0)
            if (completionWindow != null)
            {
                if (!char.IsLetterOrDigit(e.Text[0]))
                {
                    // Avoid inserting the same text twice!
                    if (completionWindow.CompletionList.SelectedItem?.Text.EndsWith(e.Text) == true)
                        e.Handled = true;
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
                            if (fragment.Length > 2 && char.IsLetterOrDigit(fragment, fragment.Length - 2))
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
        if (e.Text == ".")
            ShowCodeCompletion(RootModel.Instance.GetMembers(GetFragment()));
        else if (e.Text == ":")
            ShowCodeCompletion(RootModel.Instance.GetClassMembers(GetFragment()));
        else if (e.Text == "(")
            ShowCodeCompletion(RootModel.Instance.GetRoots(avalon.CaretOffset, avalon.Text));
    }

    private string GetFragment(int delta = 1) => avalon.Document.GetText(0, avalon.CaretOffset - delta);

    private void ShowCodeCompletion(IList<Member> list)
    {
        if (list?.Count > 0)
        {
            completionWindow = new CompletionWindow(avalon.TextArea);
            IList<ICompletionData> data = completionWindow.CompletionList.CompletionData;
            foreach ((string member, string description) in list)
                data.Add(new CompletionData(member, description));
            completionWindow.Show();
            completionWindow.Closed += CompletionListClosed;
        }
    }

    private void CompletionListClosed(object? sender, EventArgs e) => completionWindow = null;

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

    private void AvalonPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.G && Keyboard.Modifiers == ModifierKeys.Control)
        {
            gMode = !gMode;
            e.Handled = true;
        }
        else if (gMode && e.Key != Key.LeftShift & e.Key != Key.RightShift)
        {
            if (Keyboard.Modifiers == ModifierKeys.None && tmgLo.TryGetValue(e.Key, out char ch)
                || Keyboard.Modifiers == ModifierKeys.Shift && tmgUp.TryGetValue(e.Key, out ch))
            {
                avalon.SelectedText = "";
                avalon.Document.Insert(avalon.CaretOffset, ch.ToString());
                e.Handled = true;
            }
            gMode = false;
        }
    }
}
