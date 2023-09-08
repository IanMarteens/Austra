namespace Ostara;

/// <summary>Interaction logic for HierarchyView.xaml</summary>
public partial class HierarchyView : UserControl
{
    public HierarchyView() => InitializeComponent();

    private void TreeViewDoubleClick(object sender, RoutedEventArgs e)
    {
        if (sender is TreeViewItem item &&
            item.IsSelected &&
            item.DataContext is NodeBase node)
            node.Show();
    }
}
