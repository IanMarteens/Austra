namespace Austra;

public abstract class CommonVectorNode<T>: VarNode<T> where T: struct
{
    protected CommonVectorNode(string formula, T value) :
        base(formula, value)
    { }

    public CommonVectorNode(ClassNode parent, string varName, T value) :
        base(parent, varName, value)
    { }

    public override string TypeName => "Vector";

    public sealed override Visibility ImageVisibility => Visibility.Visible;

    public sealed override string ImageSource => "/images/vector.png";
}

public sealed class VectorNode: CommonVectorNode<RVector>
{
    public VectorNode(string formula, RVector value) :
        base(formula, value)
    { }

    public VectorNode(ClassNode parent, string varName, RVector value) :
        base(parent, varName, value)
    { }

    public override string Hint => $"{Name} ∊ ℝ({Model.Length})";
}

public sealed class CVectorNode : CommonVectorNode<ComplexVector>
{
    public CVectorNode(string formula, ComplexVector value) :
        base(formula, value)
    { }

    public CVectorNode(ClassNode parent, string varName, ComplexVector value) :
        base(parent, varName, value)
    { }

    public override string Hint => $"{Name} ∊ ℂ({Model.Length})";
}