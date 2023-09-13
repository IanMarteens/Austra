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
        row.Cells.Add(NewHdr("λ"));
        row.Cells.Add(NewHdr("Return"));
        row.Cells.Add(NewHdr("Volatility"));
        foreach (string name in Model.Labels)
            row.Cells.Add(NewHdr(name));
        group.Rows.Add(row);
        for (int i = 0; i < Model.Length; i++)
        {
            Portfolio p = Model[i];
            row = new();
            row.Cells.Add(NewCell(p.Lambda.ToString("F2")));
            row.Cells.Add(NewCell(p.Mean.ToString("G6")));
            row.Cells.Add(NewCell(p.StdDev.ToString("G6")));
            for (int j = 0; j < Model.Size; j++)
                row.Cells.Add(NewCell(p.Weights[j].ToString("F6")));
            group.Rows.Add(row);
        }
        result.RowGroups.Add(group);
        return result;

        static TableCell NewCell(string text) => new(new Paragraph(new Run(text))
        {
            TextAlignment = TextAlignment.Right
        });

        static TableCell NewHdr(string header) => new(new Paragraph(new Run(header))
        {
            FontWeight = FontWeights.DemiBold,
            TextAlignment = TextAlignment.Right
        });
    }
}

