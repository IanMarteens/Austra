namespace Austra;

/// <summary>Interaction logic for OptionsView.xaml</summary>
public partial class OptionsView : Window
{
    public OptionsView()
    {
        InitializeComponent();
        Owner = Application.Current.MainWindow;
        ctrlAutoload.IsChecked = Properties.Settings.Default.Autoload;
        ctrlFile.Text = Properties.Settings.Default.AutoloadFile;
        ctrlCompTime.IsChecked = Properties.Settings.Default.ShowCompileTime;
        ctrlExecTime.IsChecked = Properties.Settings.Default.ShowExecutionTime;
        ctrlDebug.IsChecked = Properties.Settings.Default.DebugFormulas;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (DialogResult == true)
        {
            Properties.Settings.Default.Autoload = ctrlAutoload.IsChecked == true;
            Properties.Settings.Default.ShowCompileTime = ctrlCompTime.IsChecked == true;
            Properties.Settings.Default.ShowExecutionTime = ctrlExecTime.IsChecked == true;
            Properties.Settings.Default.DebugFormulas = ctrlDebug.IsChecked == true;
            Properties.Settings.Default.Save();
            if (RootModel.Instance?.Environment?.Engine is not null)
                RootModel.Instance.Environment.Engine.DebugFormulas = Properties.Settings.Default.DebugFormulas;
        }
    }

    private void GridMouseDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void CloseDlg(object sender, RoutedEventArgs e) => DialogResult = false;

    private void OkClose(object sender, RoutedEventArgs e) => DialogResult = true;
}
