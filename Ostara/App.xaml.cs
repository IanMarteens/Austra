namespace Ostara;

/// <summary>Interaction logic for App.xaml</summary>
public partial class App : Application
{
    private void Application_DispatcherUnhandledException(
        object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Exception x = e.Exception;
        if (x.InnerException != null)
            x = x.InnerException;
        var ax = x as AggregateException;
        if (ax?.InnerExceptions.Count > 0)
            x = ax.InnerExceptions[0];
        string message = x.Message;
        MessageBox.Show(
            message,
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Stop);
        e.Handled = true;
    }
}
