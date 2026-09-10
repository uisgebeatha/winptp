using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WinPTP.Printer;
using WinPTP.Rendering;

namespace WinPTP;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Typeface _labelTypeface = new("Segoe UI");
    private readonly TextLabelLayout _labelLayout = TextLabelLayout.TwelveMillimeter;
    private LabelRaster? _previewRaster;
    private bool _controlsEnabled = true;

    public MainWindow()
    {
        InitializeComponent();
        LabelTextBox.TextChanged += LabelTextBox_TextChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        UpdatePreview();
        RefreshPorts();
    }

    private void LabelTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        RefreshPorts();
    }

    private async void CheckPrinterButton_Click(object sender, RoutedEventArgs e)
    {
        if (PortComboBox.SelectedItem is not string portName)
        {
            StatusTextBlock.Text = "Select a COM port before checking the printer.";
            return;
        }

        SetControlsEnabled(false);
        StatusTextBlock.Text = $"Checking for a PT-P300BT on {portName}...";

        try
        {
            PtP300BtStatus status = await Task.Run(() => PtP300BtClient.QueryStatus(portName));
            StatusTextBlock.Text = FormatStatus(portName, status);
        }
        catch (PtP300BtResponseException ex)
        {
            StatusTextBlock.Text = $"Printer check failed on {portName}: {ex.Message}";
        }
        catch (TimeoutException ex)
        {
            StatusTextBlock.Text = $"Printer check timed out on {portName}: {ex.Message}";
        }
        catch (UnauthorizedAccessException)
        {
            StatusTextBlock.Text = $"Cannot open {portName}. The port may be in use or access was denied.";
        }
        catch (IOException ex)
        {
            StatusTextBlock.Text = $"Serial communication failed on {portName}: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            StatusTextBlock.Text = $"Cannot use {portName}: {ex.Message}";
        }
        catch (ArgumentException ex)
        {
            StatusTextBlock.Text = $"Cannot use {portName}: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Printer check failed on {portName}: {ex.Message}";
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private async void PrintButton_Click(object sender, RoutedEventArgs e)
    {
        if (PortComboBox.SelectedItem is not string portName)
        {
            StatusTextBlock.Text = "Select a COM port before printing.";
            return;
        }

        if (_previewRaster is not LabelRaster raster)
        {
            StatusTextBlock.Text = "Enter label text before printing.";
            return;
        }

        string labelText = LabelTextBox.Text;
        SetControlsEnabled(false);
        StatusTextBlock.Text = $"Printing the current preview on {portName}...";

        try
        {
            PtP300BtPrintResult result = await Task.Run(() => PtP300BtClient.Print(portName, raster));

            switch (result.Outcome)
            {
                case PtP300BtPrintOutcome.PrintingCompleted:
                    StatusTextBlock.Text =
                        $"Printed \"{labelText}\" successfully on {portName}.\n" +
                        $"Printer: {result.Description}";
                    break;
                case PtP300BtPrintOutcome.Printing:
                    StatusTextBlock.Text =
                        $"Print command sent to {portName}. Printer: {result.Description}. " +
                        "Completion status was not received before the wait expired.";
                    break;
                case PtP300BtPrintOutcome.PrinterError:
                    StatusTextBlock.Text = $"Print failed on {portName}: {result.Description}.";
                    break;
                case PtP300BtPrintOutcome.CompletionStatusNotReceived:
                    StatusTextBlock.Text =
                        $"Print command sent to {portName}, but completion status was not received before the wait expired.";
                    break;
                default:
                    StatusTextBlock.Text =
                        $"Print command sent to {portName}. {result.Description}. Completion was not confirmed.";
                    break;
            }
        }
        catch (PtP300BtPrintException ex)
        {
            StatusTextBlock.Text = $"Print failed on {portName}: {ex.Message}";
        }
        catch (PtP300BtResponseException ex)
        {
            StatusTextBlock.Text = $"Print failed on {portName}: {ex.Message}";
        }
        catch (TimeoutException ex)
        {
            StatusTextBlock.Text = $"Print timed out on {portName}: {ex.Message}";
        }
        catch (UnauthorizedAccessException)
        {
            StatusTextBlock.Text = $"Cannot open {portName}. The port may be in use or access was denied.";
        }
        catch (IOException ex)
        {
            StatusTextBlock.Text = $"Serial communication failed on {portName}: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            StatusTextBlock.Text = $"Cannot use {portName}: {ex.Message}";
        }
        catch (ArgumentException ex)
        {
            StatusTextBlock.Text = $"Cannot use {portName}: {ex.Message}";
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Print failed on {portName}: {ex.Message}";
        }
        finally
        {
            SetControlsEnabled(true);
        }
    }

    private void UpdatePreview()
    {
        string text = LabelTextBox.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            _previewRaster = null;
            PreviewImage.Source = null;
            LabelLengthTextBlock.Text = "Label length: 0.0 mm";
            PreviewPlaceholderTextBlock.Text = "Enter label text to generate a preview.";
            PreviewPlaceholderTextBlock.Visibility = Visibility.Visible;
            UpdatePrintButtonState();
            return;
        }

        try
        {
            TextLabelRenderResult rendered = TextLabelRasterizer.Render(text, _labelTypeface, _labelLayout);
            _previewRaster = rendered.Raster;
            PreviewImage.Source = LabelRasterPreviewConverter.ToBitmapSource(rendered.Raster);
            LabelLengthTextBlock.Text = $"Label length: {rendered.Raster.LengthMillimeters:F1} mm";
            PreviewPlaceholderTextBlock.Visibility = Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            _previewRaster = null;
            PreviewImage.Source = null;
            LabelLengthTextBlock.Text = "Label length: —";
            PreviewPlaceholderTextBlock.Text = $"Preview unavailable: {ex.Message}";
            PreviewPlaceholderTextBlock.Visibility = Visibility.Visible;
        }

        UpdatePrintButtonState();
    }

    private void RefreshPorts()
    {
        string? previousSelection = PortComboBox.SelectedItem as string;

        try
        {
            IReadOnlyList<string> ports = SerialPortDiscovery.GetPortNames();
            PortComboBox.ItemsSource = ports;

            if (previousSelection is not null && ports.Contains(previousSelection))
            {
                PortComboBox.SelectedItem = previousSelection;
            }
            else if (ports.Count > 0)
            {
                PortComboBox.SelectedIndex = 0;
            }

            StatusTextBlock.Text = ports.Count == 0
                ? "No serial COM ports were found. Pair the printer in Windows and refresh."
                : $"Found {ports.Count} serial COM port{(ports.Count == 1 ? string.Empty : "s")}.";
        }
        catch (Exception ex)
        {
            PortComboBox.ItemsSource = Array.Empty<string>();
            StatusTextBlock.Text = $"Could not enumerate serial COM ports: {ex.Message}";
        }
    }

    private void SetControlsEnabled(bool isEnabled)
    {
        _controlsEnabled = isEnabled;
        LabelTextBox.IsEnabled = isEnabled;
        PortComboBox.IsEnabled = isEnabled;
        RefreshButton.IsEnabled = isEnabled;
        CheckPrinterButton.IsEnabled = isEnabled;
        UpdatePrintButtonState();
    }

    private void UpdatePrintButtonState()
    {
        PrintButton.IsEnabled = _controlsEnabled && _previewRaster is not null;
    }

    private static string FormatStatus(string portName, PtP300BtStatus status)
    {
        StringBuilder result = new();
        result.AppendLine("Printer: PT-P300BT");
        result.AppendLine($"COM port: {portName}");
        result.AppendLine($"Tape width: {status.TapeWidthMillimeters} mm");
        result.AppendLine($"Tape/media type: {status.MediaTypeDescription} (0x{status.MediaType:X2})");
        result.Append($"State: {status.StateDescription}");
        return result.ToString();
    }
}
