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
    private const string AutoSizeMode = "Auto";
    private const string ManualSizeMode = "Manual";

    private readonly TextLabelLayout _labelLayout = TextLabelLayout.TwelveMillimeter;
    private LabelRaster? _previewRaster;
    private bool _controlsEnabled = true;
    private bool _editorControlsInitialized;
    private bool _updatingFontSizeControl;

    public MainWindow()
    {
        InitializeComponent();
        LabelTextBox.TextChanged += LabelTextBox_TextChanged;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeEditorControls();
        UpdatePreview();
        RefreshPorts();
    }

    private void LabelTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void FontComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_editorControlsInitialized)
        {
            UpdatePreview();
        }
    }

    private void SizeModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_editorControlsInitialized)
        {
            return;
        }

        UpdateFontSizeControlState();
        UpdatePreview();
    }

    private void FontSizeSlider_ValueChanged(
        object sender,
        RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_editorControlsInitialized || _updatingFontSizeControl)
        {
            return;
        }

        FontSizeValueTextBlock.Text = $"{FontSizeSlider.Value:F1}";
        if (IsManualSizeMode)
        {
            UpdatePreview();
        }
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
            FontSizeValueTextBlock.Text = IsManualSizeMode
                ? $"{FontSizeSlider.Value:F1}"
                : "—";
            PreviewPlaceholderTextBlock.Text = "Enter label text to generate a preview.";
            PreviewPlaceholderTextBlock.Visibility = Visibility.Visible;
            UpdatePrintButtonState();
            return;
        }

        try
        {
            TextLabelRenderResult rendered = TextLabelRasterizer.Render(
                text,
                GetSelectedTypeface(),
                _labelLayout,
                IsManualSizeMode ? FontSizeSlider.Value : null);
            _previewRaster = rendered.Raster;
            PreviewImage.Source = LabelRasterPreviewConverter.ToBitmapSource(rendered.Raster);
            LabelLengthTextBlock.Text = $"Label length: {rendered.Raster.LengthMillimeters:F1} mm";
            SetEffectiveFontSize(rendered.SelectedFontSizeDots);
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
        FontComboBox.IsEnabled = isEnabled;
        SizeModeComboBox.IsEnabled = isEnabled;
        PortComboBox.IsEnabled = isEnabled;
        RefreshButton.IsEnabled = isEnabled;
        CheckPrinterButton.IsEnabled = isEnabled;
        UpdateFontSizeControlState();
        UpdatePrintButtonState();
    }

    private void UpdatePrintButtonState()
    {
        PrintButton.IsEnabled = _controlsEnabled && _previewRaster is not null;
    }

    private bool IsManualSizeMode =>
        string.Equals(SizeModeComboBox.SelectedItem as string, ManualSizeMode, StringComparison.Ordinal);

    private void InitializeEditorControls()
    {
        if (_editorControlsInitialized)
        {
            return;
        }

        IReadOnlyList<InstalledFontFamily> installedFonts =
            InstalledFontCatalog.GetInstalledFamilies();
        FontComboBox.ItemsSource = installedFonts;
        FontComboBox.SelectedItem = installedFonts.FirstOrDefault(font =>
                string.Equals(font.DisplayName, "Segoe UI", StringComparison.CurrentCultureIgnoreCase))
            ?? installedFonts.FirstOrDefault();

        SizeModeComboBox.ItemsSource = new[] { AutoSizeMode, ManualSizeMode };
        SizeModeComboBox.SelectedItem = AutoSizeMode;
        FontSizeSlider.Maximum = _labelLayout.TextAreaHeightDots;
        _editorControlsInitialized = true;
        UpdateFontSizeControlState();
    }

    private Typeface GetSelectedTypeface()
    {
        return FontComboBox.SelectedItem is InstalledFontFamily selectedFont
            ? selectedFont.CreateTypeface()
            : new Typeface("Segoe UI");
    }

    private void SetEffectiveFontSize(double fontSizeDots)
    {
        _updatingFontSizeControl = true;
        try
        {
            FontSizeSlider.Value = fontSizeDots;
            FontSizeValueTextBlock.Text = $"{fontSizeDots:F1}";
        }
        finally
        {
            _updatingFontSizeControl = false;
        }
    }

    private void UpdateFontSizeControlState()
    {
        FontSizeSlider.IsEnabled = _controlsEnabled && IsManualSizeMode;
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
