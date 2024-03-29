﻿using Austra.Parser;
using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using System.IO;

namespace Austra;

/// <summary>Interaction logic for MainWindow.xaml</summary>
public partial class MainWindow : Window
{
    private CompletionWindow? completionWindow;
    private bool gMode, qMode;

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

    private RootModel Root => (RootModel)DataContext;

    public void CloseCompletion() => completionWindow?.Close();

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

    private void TextArea_TextEntered(object sender, TextCompositionEventArgs e) =>
        ShowCodeCompletion(e.Text == "."
            ? RootModel.Instance.GetMembers(GetFragment())
            : e.Text == ":"
            ? RootModel.Instance.GetClassMembers(GetFragment())
            : e.Text == "("
            ? RootModel.Instance.GetRoots(avalon.CaretOffset, avalon.Text)
            : null);

    private string GetFragment(int delta = 1) => avalon.Document.GetText(0, avalon.CaretOffset - delta);

    private void ShowCodeCompletion(IList<Member>? list)
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

    private void DocumentChanged(DocumentChangeEventArgs e)
    {
        if (e.InsertedText.Text.EndsWith('('))
            RootModel.Instance.ShowParameterInfo(GetFragment(1));
        else if (e.InsertedText.Text.EndsWith(')'))
            RootModel.Instance.HideParameterInfo();
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
        else if (e.Key == Key.Q && Keyboard.Modifiers == ModifierKeys.Control)
        {
            qMode = !qMode;
            e.Handled = true;
        }
        else if (gMode && e.Key != Key.LeftShift & e.Key != Key.RightShift)
        {
            if (GreekSymbols.TryTransform(e.Key, out char ch))
            {
                avalon.SelectedText = "";
                avalon.Document.Insert(avalon.CaretOffset, ch.ToString());
                e.Handled = true;
            }
            gMode = false;
        }
        else if (qMode && e.Key != Key.LeftShift & e.Key != Key.RightShift)
        {
            if (e.Key == Key.A)
            {
                avalon.SelectedText = "";
                avalon.Document.Insert(avalon.CaretOffset, "∀");
                e.Handled = true;
            }
            else if (e.Key == Key.E)
            {
                avalon.SelectedText = "";
                avalon.Document.Insert(avalon.CaretOffset, "∃");
                e.Handled = true;
            }
            else if (e.Key == Key.I)
            {
                avalon.SelectedText = "";
                avalon.Document.Insert(avalon.CaretOffset, "∈");
                e.Handled = true;
            }
            qMode = false;
        }
    }

    private void OverloadDownClick(object sender, MouseButtonEventArgs e) => RootModel.Instance.OverloadDown.Execute(null);

    private void OverloadUpClick(object sender, MouseButtonEventArgs e) => RootModel.Instance.OverloadUp.Execute(null);
}
