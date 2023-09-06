namespace Ostara;

/// <summary>Represents a session variable.</summary>
public abstract class VarNode : NodeBase
{
    public VarNode(ClassNode parent, string varName, Type type) =>
        (Parent, VarName, Formula, Type) = (parent, varName, varName, type);

    public VarNode(ClassNode? parent, string varName, string formula, Type type) =>
        (Parent, VarName, Formula, Type) = (parent, varName, formula, type);

    public ClassNode? Parent { get; }
    public string VarName { get; }
    public Type Type { get; }
    public string Formula { get; }

    public bool Stored { get; init; }

    public Visibility StoredVisibility =>
        Stored ? Visibility.Visible : Visibility.Collapsed;

    public virtual string DisplayName => VarName;

    public Visibility IsOrphan =>
        Parent != null ? Visibility.Collapsed : Visibility.Visible;

    protected virtual string GetExcelText() => "";
}


