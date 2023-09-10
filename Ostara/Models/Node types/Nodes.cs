﻿using Austra.Parser;

namespace Ostara;

/// <summary>Represents a node in the entities tree.</summary>
public abstract class NodeBase : Entity
{
    private bool isSelected;
    private bool isExpanded;

    protected NodeBase(string name, string type) => (Name, TypeName) = (name, type);

    /// <summary>Gets or set whether the corresponding tree node is selected.</summary>
    public bool IsSelected
    {
        get => isSelected;
        set => SetField(ref isSelected, value);
    }

    /// <summary>Gets or set whether the corresponding tree node is expanded.</summary>
    public bool IsExpanded
    {
        get => isExpanded;
        set => SetField(ref isExpanded, value);
    }

    /// <summary>Shows the corresponding view in the main window.</summary>
    public virtual void Show() { }

    [Category("ID")]
    public string Name { get; }

    [Category("ID")]
    public string TypeName { get; }
}

/// <summary>Represents a class node, grouping variables of the same type.</summary>
public class ClassNode : NodeBase
{
    public ClassNode(string className) : this(className, "ClassNode") { }

    public ClassNode(string className, string type) : base(className, type) =>
        Order = className switch
        {
            "Series" => 0,
            "Matrix" => 1,
            "Vector" => 2,
            _ => 3,
        };

    public ObservableCollection<VarNode> Nodes { get; } = new();

    public int Order { get; }
}

/// <summary>Represents an class node grouping AUSTRA definitions.</summary>
public class AllDefinitionsNode : ClassNode
{
    public AllDefinitionsNode() : base("Definitions", "Definition node") { }
}

/// <summary>Represents a session variable.</summary>
public abstract class VarNode : NodeBase
{
    protected VarNode(ClassNode? parent, string name, string formula, string type) :
        base(name, type) =>
        (Parent, Formula) = (parent, formula);

    public ClassNode? Parent { get; }
    public string Formula { get; }

    public bool Stored { get; init; }

    public Visibility StoredVisibility =>
        Stored ? Visibility.Visible : Visibility.Collapsed;

    public Visibility IsOrphan =>
        Parent != null ? Visibility.Collapsed : Visibility.Visible;

    protected virtual string GetExcelText() => "";

    public virtual string Hint => $"{Name}: {TypeName}";
}

/// <summary>A catch-all variable node, for variables that are not of a specific type.</summary>
public class MiscNode : VarNode
{
    public MiscNode(ClassNode parent, string varName, Type type, string value) :
        base(parent, varName, varName, type.Name) =>
        Value = value;

    public override void Show()
    {
        if (Parent != null)
        {
            Parent.IsExpanded = true;
            IsSelected = true;
        }
        RootModel.Instance.AppendResult(Name, Value);
    }

    [Category("Content")]
    public string Value { get; }
}

public sealed class DefinitionNode : VarNode
{
    public DefinitionNode(AllDefinitionsNode parent, Definition def) :
        base(parent, def.Name, def.Name, def.Type == typeof(ARSModel) || def.Type == typeof(ARVModel)
            ? "ARModel"
            : def.Type == typeof(LinearSModel) || def.Type == typeof(LinearVModel)
            ? "LinearModel"
            : def.Type == typeof(FftCModel) || def.Type == typeof(FftRModel)
            ? "FFT Model"
            : def.Type.Name)
    {
        Body = def.Text;
        Description = def.Description;
    }

    override public void Show() =>
        RootModel.Instance.Evaluate(Name);

    [Category("Content")]
    public string Body { get; }

    [Category("Content")]
    public string Description { get; }

    public override string Hint => Description == Name ? TypeName : Description;
}
