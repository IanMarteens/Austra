namespace Austra;

public class MatrixNode : VarNode<AMatrix>
{
    private readonly sbyte triangularity;

    public MatrixNode(ClassNode? parent, string varName, string formula, AMatrix value) :
        base(parent, varName, formula, "Matrix", value)
    { }

    public MatrixNode(ClassNode? parent, string varName, AMatrix value) :
        this(parent, varName, varName, value)
    { }

    public MatrixNode(ClassNode? parent, string varName, string formula, LMatrix value) :
        base(parent, varName, formula, "Matrix", (Matrix)value) => triangularity = -1;

    public MatrixNode(ClassNode? parent, string varName, LMatrix value) :
        this(parent, varName, varName, value)
    { }

    public MatrixNode(ClassNode? parent, string varName, string formula, RMatrix value) :
        base(parent, varName, formula, "Matrix", (Matrix)value) => triangularity = +1;

    public MatrixNode(ClassNode? parent, string varName, RMatrix value) :
        this(parent, varName, varName, value)
    { }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/matrix.png";

    public override string Hint => $"{Name} ∊ ℝ({Model.Rows}⨯{Model.Cols})";

    public override void Show() =>
        RootModel.Instance.AppendResult(Formula,
            $"ans ∊ ℝ({Model.Rows}⨯{Model.Cols})" + Environment.NewLine +
            ((double[])Model).ToString(Model.Rows, Model.Cols,
                v => v.ToString("G6"), triangularity));
}
