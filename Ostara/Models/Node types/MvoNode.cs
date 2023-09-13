using System.Windows.Documents;

namespace Ostara;

public sealed class MvoNode : VarNode<MvoModel>
{
    public MvoNode(ClassNode? parent, string varName, string formula, MvoModel value) :
    base(parent, varName, formula, "MVO Model", value)
    { }

    public MvoNode(ClassNode? parent, string varName, MvoModel value) :
        this(parent, varName, varName, value)
    { }

    public override Visibility ImageVisibility => Visibility.Visible;

    public override string ImageSource => "/images/mvo.png";

    public override void Show() => RootModel.Instance.AppendResult(Formula, CreateTable());

    private Table CreateTable()
    {
        Table result = new();
        for (int i = 0; i < 3 + Model.Size; i++)
            result.Columns.Add(new TableColumn() { Width = new GridLength(100, GridUnitType.Pixel) });
        TableRowGroup group = new();
        TableRow row = new();
        row.Cells.Add(new TableCell(new Paragraph(new Run("Return") { FontWeight = FontWeights.DemiBold })));
        row.Cells.Add(new TableCell(new Paragraph(new Run("Volatility") { FontWeight = FontWeights.DemiBold })));
        row.Cells.Add(new TableCell(new Paragraph(new Run("Lambda") { FontWeight = FontWeights.DemiBold })));
        foreach (string name in Model.Labels)
            row.Cells.Add(new TableCell(new Paragraph(new Run(name) { FontWeight = FontWeights.DemiBold })));
        group.Rows.Add(row);
        for (int i = 0; i < Model.Length; i++)
        {
            row = new();
            row.Cells.Add(new TableCell(new Paragraph(new Run(Model.Portfolios[i].Mean.ToString("G6")))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(Model.Portfolios[i].StdDev.ToString("G6")))));
            row.Cells.Add(new TableCell(new Paragraph(new Run(Model.Portfolios[i].Lambda.ToString("F2")))));
            for (int j = 0; j < Model.Size; j++)
                row.Cells.Add(new TableCell(new Paragraph(new Run(Model.Portfolios[i].Weights[j].ToString("G6")))));
            group.Rows.Add(row);
        }
        result.RowGroups.Add(group);
        return result;
    }
}

