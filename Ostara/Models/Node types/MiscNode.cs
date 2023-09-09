namespace Ostara;

public class MiscNode : VarNode
{
    public MiscNode(ClassNode parent, string varName, Type type, string value) :
        base(parent, varName, varName, type.Name) =>
        Value = value;

    public override void Show()
    {
        if (Parent != null)
        {
            Parent.IsExpanded = true;
            IsSelected = true;
        }
        RootModel.Instance.AppendResult(Name, Value);
    }

    [Category("Content")]
    public string Value { get; }
}
