namespace Ostara;

/// <summary>Represents a class node, grouping variables of the same type.</summary>
public class ClassNode : NodeBase
{
    public ClassNode(string className) =>
        (Name, Order) = (className, className switch
        {
            "Series" => 0,
            "Matrix" => 1,
            "Vector" => 2,
            _ => 3,
        });

    public ObservableCollection<VarNode> Nodes { get; } = new();

    public int Order { get; }

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; protected set; } = "Class node";
}
