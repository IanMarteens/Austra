namespace Ostara;

/// <summary>Represents a node in the entities tree.</summary>
public abstract class NodeBase : Entity
{
    private bool isSelected;
    private bool isExpanded;

    protected NodeBase(string name, string type) => (Name, TypeName) = (name, type);

    /// <summary>Gets or set whether the corresponding tree node is selected.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetField(ref isSelected, value);
    }

    /// <summary>Gets or set whether the corresponding tree node is expanded.</summary>
    public bool IsExpanded
    {
        get => isExpanded;
        set => SetField(ref isExpanded, value);
    }

    /// <summary>Shows the corresponding view in the main window.</summary>
    public virtual void Show() { }

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }
}
