using System.Windows.Data;

namespace Austra;

/// <summary>Interaction logic for VSplineView.xaml</summary>
public partial class VSplineView : UserControl
{
    private bool gMode;

    public VSplineView() => InitializeComponent();

    private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BindingExpression be = newValue.GetBindingExpression(TextBox.TextProperty);
            be?.UpdateSource();
        }
        else if (e.Key == Key.G && Keyboard.Modifiers == ModifierKeys.Control)
        {
            gMode = !gMode;
            e.Handled = true;
        }
        else if (gMode && e.Key != Key.LeftShift & e.Key != Key.RightShift)
        {
            if (GreekSymbols.TryTransform(e.Key, out char ch))
            {
                newValue.SelectedText = ch.ToString();
                newValue.SelectionLength = 0;
                e.Handled = true;
            }
            gMode = false;
        }
    }
}
