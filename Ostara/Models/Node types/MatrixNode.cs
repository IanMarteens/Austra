namespace Ostara;

public class MatrixNode: VarNode<Matrix>
{
    private readonly sbyte triangularity;

    public MatrixNode(ClassNode? parent, string varName, string formula, Matrix value) :
        base(parent, varName, formula, "Matrix", value)
    { }

    public MatrixNode(ClassNode? parent, string varName, Matrix value) :
        this(parent, varName, varName, value)
    { }

    public MatrixNode(ClassNode? parent, string varName, string formula, LMatrix value) :
        base(parent, varName, formula, "Matrix", (double[,])value) => triangularity = -1;

    public MatrixNode(ClassNode? parent, string varName, LMatrix value) :
        this(parent, varName, varName, value)
    { }

    public MatrixNode(ClassNode? parent, string varName, string formula, RMatrix value) :
        base(parent, varName, formula, "Matrix", (double[,])value) => triangularity = +1;

    public MatrixNode(ClassNode? parent, string varName, RMatrix value) :
        this(parent, varName, varName, value)
    { }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/matrix.png";

    public override string Hint => $"{Name} ∊ ℝ({Model.Rows}⨯{Model.Cols})";

    public override void Show() => 
        RootModel.Instance.AppendResult(Formula, CommonMatrix.ToString((double[,])Model,
            v => v.ToString("G6"), triangularity));
}
