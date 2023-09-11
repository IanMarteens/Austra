namespace Ostara;

public abstract class CommonVectorNode<T>: VarNode<T> where T: struct
{
    protected CommonVectorNode(ClassNode? parent, string varName, string formula, T value) :
        base(parent, varName, formula, "Vector", value)
    { }

    public CommonVectorNode(ClassNode? parent, string varName, T value) :
        this(parent, varName, varName, value)
    { }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/vector.png";

    public override void Show() =>
        RootModel.Instance.AppendResult(Formula, Model.ToString());
}

public sealed class VectorNode: CommonVectorNode<RVector>
{
    public VectorNode(ClassNode? parent, string varName, string formula, RVector value) :
        base(parent, varName, formula, value)
    { }

    public VectorNode(ClassNode? parent, string varName, RVector value) :
        this(parent, varName, varName, value)
    { }

    public override string Hint => $"{Name} ∊ ℝ({Model.Length})";
}

public sealed class CVectorNode : CommonVectorNode<ComplexVector>
{
    public CVectorNode(ClassNode? parent, string varName, string formula, ComplexVector value) :
        base(parent, varName, formula, value)
    { }

    public CVectorNode(ClassNode? parent, string varName, ComplexVector value) :
        this(parent, varName, varName, value)
    { }

    public override string Hint => $"{Name} ∊ ℂ({Model.Length})";
}