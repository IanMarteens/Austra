using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Austra;

public record class CompletionData(string Text, object Description) : ICompletionData
{
    public System.Windows.Media.ImageSource? Image => null;

    public object Content => Text;

    public double Priority => 0;

    public void Complete(
        TextArea textArea,
        ISegment completionSegment,
        EventArgs insertionRequestEventArgs) =>
        textArea.Document.Replace(completionSegment, Text);
}

public sealed class InsightProvider(IReadOnlyList<string> overloads) : IOverloadProvider
{
    public int SelectedIndex { 
        get;
        set {
            if (field != value)
            {
                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedIndex)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentIndexText)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentContent)));
            }
        }
    }

    public int Count => overloads.Count;

    public string CurrentIndexText => $"{SelectedIndex + 1} of {Count}";

    public object CurrentHeader => "";

    public object CurrentContent =>
        (uint)SelectedIndex < Count ? overloads[SelectedIndex] : "";

    public event PropertyChangedEventHandler? PropertyChanged;
}
