﻿namespace Austra;

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

public sealed class VectorNode: CommonVectorNode<DVector>
{
    public VectorNode(string formula, DVector value) :
        base(formula, value)
    { }

    public VectorNode(ClassNode parent, string varName, DVector value) :
        base(parent, varName, value)
    { }

    public override string Hint => $"{Name} ∊ ℝ({Model.Length})";
}

public sealed class CVectorNode : CommonVectorNode<CVector>
{
    public CVectorNode(string formula, CVector value) :
        base(formula, value)
    { }

    public CVectorNode(ClassNode parent, string varName, CVector value) :
        base(parent, varName, value)
    { }

    public override string Hint => $"{Name} ∊ ℂ({Model.Length})";
}