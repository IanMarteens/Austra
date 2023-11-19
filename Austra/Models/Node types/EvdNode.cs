namespace Austra;

public sealed class EvdNode : VarNode<EVD>
{
    public EvdNode(string formula, EVD value) :
        base(formula, value)
    { }

    public EvdNode(ClassNode parent, string varName, EVD value) :
        base(parent, varName, value)
    { }

    public override string TypeName => "EVD";

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/evd.png";
}
