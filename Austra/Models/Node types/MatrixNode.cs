namespace Austra;

public class MatrixNode : VarNode<AMatrix>
{
    private readonly sbyte triangularity;

    public MatrixNode(string formula, AMatrix value) :
        base(formula, value)
    { }

    public MatrixNode(ClassNode parent, string varName, AMatrix value) :
        base(parent, varName, value)
    { }

    public MatrixNode(string formula, LMatrix value) :
        base(formula, (AMatrix)value) => triangularity = -1;

    public MatrixNode(ClassNode parent, string varName, LMatrix value) :
        base(parent, varName, (AMatrix)value)
    { }

    public MatrixNode(string formula, RMatrix value) :
        base(formula, (AMatrix)value) => triangularity = +1;

    public MatrixNode(ClassNode parent, string varName, RMatrix value) :
        base(parent, varName, (AMatrix)value)
    { }

    public override string TypeName => "Matrix";

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/matrix.png";

    public override string Hint => $"{Name} ∊ ℝ({Model.Rows}⨯{Model.Cols})";

    public override void Show() =>
        RootModel.Instance.AppendResult(Formula,
            $"ans ∊ ℝ({Model.Rows}⨯{Model.Cols})" + Environment.NewLine +
            ((double[])Model).ToString(Model.Rows, Model.Cols,
                v => v.ToString("G6"), triangularity));
}
