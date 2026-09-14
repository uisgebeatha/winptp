using System.Windows;

namespace WinPTP;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        ApplicationMetadata metadata = ApplicationMetadata.Current;
        VersionTextBlock.Text = $"Version {metadata.Version}";
        BuildDateTextBlock.Text = $"Build date {metadata.BuildDateText}";
    }
}
