using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;

namespace Ostara;

public class CompletionData : ICompletionData
{
    public CompletionData(string text, string descr) =>
        (Text, Description) = (text, descr);

    public System.Windows.Media.ImageSource? Image => null;

    public string Text { get; }

    public object Content => Text;

    public object Description { get; }

    public double Priority => 0;

    public void Complete(
        TextArea textArea,
        ISegment completionSegment,
        EventArgs insertionRequestEventArgs) =>
        textArea.Document.Replace(completionSegment, Text);
}
