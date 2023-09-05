using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostara;

public class MiscNode : VarNode
{
    public MiscNode(ClassNode parent, string varName, Type type, string value) :
        base(parent, varName, type) =>
        (Name, TypeName, Value) = (varName, type.Name, value);

    public override void Show()
    {
        if (Parent != null)
        {
            Parent.IsExpanded = true;
            IsSelected = true;
        }
        RootModel.Instance.AppendResult(VarName, Value);
    }

    public override string DisplayName => $"{VarName}: {TypeName}";

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }

    [Category("Content")]
    public string Value { get; }
}
