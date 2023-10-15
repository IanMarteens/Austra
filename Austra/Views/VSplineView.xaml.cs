using System.Windows.Data;

namespace Austra;

/// <summary>Interaction logic for VSplineView.xaml</summary>
public partial class VSplineView : UserControl
{
    public VSplineView() => InitializeComponent();

    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BindingExpression be = newValue.GetBindingExpression(TextBox.TextProperty);
            be?.UpdateSource();
        }
    }
}
