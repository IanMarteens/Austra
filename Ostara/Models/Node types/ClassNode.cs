namespace Ostara;

/// <summary>Represents a class node, grouping variables of the same type.</summary>
public class ClassNode : NodeBase
{
    public ClassNode(string className) : this(className, "ClassNode") { }

    public ClassNode(string className, string type) : base(className, type) =>
        Order = className switch
        {
            "Series" => 0,
            "Matrix" => 1,
            "Vector" => 2,
            _ => 3,
        };

    public ObservableCollection<VarNode> Nodes { get; } = new();

    public int Order { get; }
}
