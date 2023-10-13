using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;

namespace Ostara;

public static class ResLoader
{
    public static IHighlightingDefinition LoadHighlightingDefinition(
           string resourceName)
    {
        var type = typeof(ResLoader);
        var fullName = type.Namespace + "." + resourceName;
        using var stream = type.Assembly.GetManifestResourceStream(fullName);
        using var reader = new XmlTextReader(stream!);
        return HighlightingLoader.Load(reader, HighlightingManager.Instance);
    }
}
