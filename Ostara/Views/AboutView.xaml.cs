namespace Ostara;

/// <summary>Interaction logic for AboutView.xaml</summary>
public partial class AboutView : Window
{
    public AboutView() => InitializeComponent();

    public string Version { get; } = "Version: " + RootModel.Version;

    private void AboutMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    private void OkClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        Close();
    }
}
