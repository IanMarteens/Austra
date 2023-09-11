namespace Ostara;

/// <summary>Interaction logic for Sparkline.xaml</summary>
public partial class Sparkline : UserControl
{
    private Series<int> series = new("", null, Array.Empty<int>(), Array.Empty<double>(), SeriesType.Raw);

    public Sparkline() => InitializeComponent();

    public Series<int> Series { get => series; set => series = value; }
}
