namespace Ostara;

public sealed class EvdNode : VarNode<EVD>
{
    public EvdNode(ClassNode? parent, string varName, string formula, EVD value) :
        base(parent, varName, formula, "EVD", value)
    { }

    public EvdNode(ClassNode? parent, string varName, EVD value) :
        this(parent, varName, varName, value)
    { }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/evd.png";

    public override void Show() =>
        RootModel.Instance.AppendResult(Formula, Model.ToString());
}
