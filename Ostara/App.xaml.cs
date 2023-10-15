using System.Globalization;
using System.Threading;

namespace Austra;

/// <summary>Interaction logic for App.xaml</summary>
public partial class App : Application
{
    /// <summary>Synthetic culture used by the UI thread.</summary>
    public static CultureInfo? GlobalCulture { get; private set; }

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        GlobalCulture = new("en-US")
        {
            DateTimeFormat = new CultureInfo("en-GB").DateTimeFormat
        };
        Thread.CurrentThread.CurrentCulture = GlobalCulture;
        Thread.CurrentThread.CurrentUICulture = GlobalCulture;
    }

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
        MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Stop);
        e.Handled = true;
    }
}
