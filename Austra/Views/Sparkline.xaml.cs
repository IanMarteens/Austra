namespace Austra;

/// <summary>Interaction logic for Sparkline.xaml</summary>
public partial class Sparkline : UserControl
{
    private Series<int> series = new("", null, [], [], SeriesType.Raw);

    public Sparkline() => InitializeComponent();

    public Series<int> Series { get => series; set => series = value; }
}
