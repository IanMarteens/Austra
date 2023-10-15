namespace Austra;

/// <summary>Interaction logic for HierarchyView.xaml</summary>
public partial class HierarchyView : UserControl
{
    public HierarchyView() => InitializeComponent();

    private void TreeViewDoubleClick(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item && item.IsSelected && item.DataContext is NodeBase node)
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (node is DefinitionNode defNode)
                    RootModel.AddNodeToEditor(defNode.Body);
                else if (node is VarNode varNode)
                    RootModel.AddNodeToEditor(varNode.Formula);
            }
            else
                node.Show();
    }
}
