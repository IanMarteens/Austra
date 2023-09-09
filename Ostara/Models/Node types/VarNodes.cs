namespace Ostara;

/// <summary>Represents a session variable.</summary>
public abstract class VarNode : NodeBase
{
    protected VarNode(ClassNode? parent, string name, string formula, string type): 
        base(name, type) =>
        (Parent, Formula) = (parent, formula);

    public ClassNode? Parent { get; }
    public string Formula { get; }

    public bool Stored { get; init; }

    public Visibility StoredVisibility =>
        Stored ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsOrphan =>
        Parent != null ? Visibility.Collapsed : Visibility.Visible;

    protected virtual string GetExcelText() => "";

    public virtual string Hint => $"{Name}: {TypeName}";
}


