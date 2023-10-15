namespace Austra;

/// <summary>Interaction logic for AboutView.xaml</summary>
public partial class AboutView : Window
{
    private bool clicked;

    public AboutView() => InitializeComponent();

    public string Version { get; } = "Version: " + RootModel.Version;

    private void AboutMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void OkClick(object sender, RoutedEventArgs? e)
    {
        clicked = true;
        Close();
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (!clicked)
            Close();
    }
}
