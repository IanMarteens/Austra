using Austra.Parser;

namespace Ostara;

public sealed class DefinitionNode : VarNode
{
    public DefinitionNode(AllDefinitionsNode parent, Definition def) :
        base(parent, def.Name, def.Type)
    {
        Name = def.Name;
        TypeName = def.Type == typeof(ARSModel) || def.Type == typeof(ARVModel)
            ? "ARModel"
            : def.Type == typeof(LinearSModel) || def.Type == typeof(LinearVModel)
            ? "LinearModel"
            : def.Type == typeof(FftCModel) || def.Type == typeof(FftRModel)
            ? "FFT Model"
            : def.Type.Name;
        Body = def.Text;
        Description = def.Description;
    }

    override public void Show() =>
        RootModel.Instance.Evaluate(Name);

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }

    [Category("Content")]
    public string Body { get; }

    [Category("Content")]
    public string Description { get; }
}
