using System.Globalization;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WinPTP.Rendering;

namespace WinPTP.Tests;

public sealed class LabelRasterTests
{
    [Theory]
    [InlineData(0, 0.0)]
    [InlineData(90, 12.7)]
    [InlineData(180, 25.4)]
    public void DotsToMillimeters_UsesOneHundredEightyDpi(int rasterDots, double expectedMillimeters)
    {
        Assert.Equal(expectedMillimeters, PrinterLengthConverter.DotsToMillimeters(rasterDots), precision: 10);
    }

    [Fact]
    public void LengthMillimeters_ChangesWithRasterLineCount()
    {
        LabelRaster shortRaster = new(90, new byte[90 * LabelRaster.BytesPerRasterLine]);
        LabelRaster longRaster = new(270, new byte[270 * LabelRaster.BytesPerRasterLine]);

        Assert.Equal(12.7, shortRaster.LengthMillimeters, precision: 10);
        Assert.Equal(38.1, longRaster.LengthMillimeters, precision: 10);
        Assert.True(longRaster.LengthMillimeters > shortRaster.LengthMillimeters);
    }

    [Fact]
    public void Pack_OneHundredTwentyEightDots_ProducesSixteenBytesMsbFirst()
    {
        bool[,] pixels = new bool[1, LabelRaster.HeadDotCount];
        pixels[0, 0] = true;
        pixels[0, 7] = true;
        pixels[0, 8] = true;
        pixels[0, 127] = true;

        LabelRaster raster = LabelRaster.Pack(pixels);
        byte[] packedLine = raster.GetRasterLine(0).ToArray();

        Assert.Equal(LabelRaster.BytesPerRasterLine, packedLine.Length);
        Assert.Equal(0x81, packedLine[0]);
        Assert.Equal(0x80, packedLine[1]);
        Assert.Equal(0x01, packedLine[15]);
        Assert.All(packedLine[2..15], value => Assert.Equal(0x00, value));
    }

    [Fact]
    public void Render_ArbitraryText_HasPrinterDimensionsAndBlankAndPrintedPixels()
    {
        TextLabelRenderResult rendered = RenderOnStaThread("Inventory shelf 42");
        LabelRaster raster = rendered.Raster;
        byte[] packedData = raster.PackedData.ToArray();

        Assert.Equal(128, raster.DotsPerRasterLine);
        Assert.Equal(16, LabelRaster.BytesPerRasterLine);
        Assert.True(raster.RasterLineCount > 0);
        Assert.Equal(raster.RasterLineCount * LabelRaster.BytesPerRasterLine, packedData.Length);
        Assert.Equal(LabelRaster.BytesPerRasterLine, raster.GetRasterLine(0).Length);
        Assert.Contains(packedData, value => value == 0x00);
        Assert.Contains(packedData, value => value != 0x00);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_EmptyOrWhitespaceText_ThrowsPredictably(string text)
    {
        Assert.Throws<ArgumentException>(() => RenderOnStaThread(text));
    }

    [Fact]
    public void Render_LongerText_ProducesLongerLabel()
    {
        TextLabelRenderResult shortLabel = RenderOnStaThread("Short");
        TextLabelRenderResult longLabel = RenderOnStaThread("A substantially longer label");

        Assert.True(longLabel.Raster.RasterLineCount > shortLabel.Raster.RasterLineCount);
    }

    [Fact]
    public void Render_ExplicitTypefaceIsUsedForMeasuredLabelWidth()
    {
        TextLabelLayout layout = TextLabelLayout.TwelveMillimeter;
        Typeface typeface = new("Consolas");
        const double fontSizeDots = 18;

        (TextLabelRenderResult Rendered, int ExpectedLength) result = RunOnStaThread(() =>
        {
            TextLabelRenderResult rendered = TextLabelRasterizer.Render(
                "iiiiiiiiii",
                typeface,
                layout,
                fontSizeDots);
            FormattedText measuredText = new(
                "iiiiiiiiii",
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                fontSizeDots,
                Brushes.Black,
                1.0);
            int expectedLength = (int)Math.Ceiling(measuredText.WidthIncludingTrailingWhitespace)
                + (layout.HorizontalPaddingDots * 2);
            return (rendered, expectedLength);
        });

        Assert.Equal(fontSizeDots, result.Rendered.SelectedFontSizeDots);
        Assert.Equal(result.ExpectedLength, result.Rendered.Raster.RasterLineCount);
    }

    [Fact]
    public void Render_DifferentTypefacesCanProduceDifferentRasterAndLength()
    {
        TextLabelRenderResult proportional = RenderOnStaThread(
            "iiiiiiiiii",
            typeface: new Typeface("Segoe UI"),
            requestedFontSizeDots: 20);
        TextLabelRenderResult monospaced = RenderOnStaThread(
            "iiiiiiiiii",
            typeface: new Typeface("Consolas"),
            requestedFontSizeDots: 20);

        Assert.NotEqual(proportional.Raster.RasterLineCount, monospaced.Raster.RasterLineCount);
        Assert.NotEqual(
            proportional.Raster.PackedData.ToArray(),
            monospaced.Raster.PackedData.ToArray());
    }

    [Fact]
    public void Render_ManualSizeIsRespectedWhenItFits()
    {
        TextLabelRenderResult rendered = RenderOnStaThread(
            "Manual size",
            requestedFontSizeDots: 18.5);

        Assert.Equal(18.5, rendered.SelectedFontSizeDots);
    }

    [Fact]
    public void Render_OversizedManualSizeClampsToAutomaticMaximum()
    {
        TextLabelRenderResult automatic = RenderOnStaThread("Clamp me");
        TextLabelRenderResult oversized = RenderOnStaThread(
            "Clamp me",
            requestedFontSizeDots: 1000);

        Assert.Equal(automatic.SelectedFontSizeDots, oversized.SelectedFontSizeDots);
        Assert.Equal(automatic.Raster.PackedData.ToArray(), oversized.Raster.PackedData.ToArray());
    }

    [Fact]
    public void Render_ChangingManualSizeChangesRasterAndLabelLength()
    {
        TextLabelRenderResult smaller = RenderOnStaThread(
            "Adjustable",
            requestedFontSizeDots: 12);
        TextLabelRenderResult larger = RenderOnStaThread(
            "Adjustable",
            requestedFontSizeDots: 24);

        Assert.True(larger.Raster.RasterLineCount > smaller.Raster.RasterLineCount);
        Assert.NotEqual(smaller.Raster.PackedData.ToArray(), larger.Raster.PackedData.ToArray());
    }

    [Fact]
    public void Render_AutomaticSizing_KeepsPrintedPixelsInsideConfiguredTextArea()
    {
        TextLabelLayout layout = TextLabelLayout.TwelveMillimeter;
        TextLabelRenderResult rendered = RenderOnStaThread("Tall gjy text", layout);

        int firstPrintedDot = LabelRaster.HeadDotCount;
        int lastPrintedDot = -1;

        for (int line = 0; line < rendered.Raster.RasterLineCount; line++)
        {
            for (int headDot = 0; headDot < LabelRaster.HeadDotCount; headDot++)
            {
                if (rendered.Raster.IsBlackPixel(line, headDot))
                {
                    firstPrintedDot = Math.Min(firstPrintedDot, headDot);
                    lastPrintedDot = Math.Max(lastPrintedDot, headDot);
                }
            }
        }

        Assert.True(rendered.SelectedFontSizeDots > 0);
        Assert.InRange(firstPrintedDot, layout.TextAreaTopDot, layout.TextAreaTopDot + layout.TextAreaHeightDots - 1);
        Assert.InRange(lastPrintedDot, layout.TextAreaTopDot, layout.TextAreaTopDot + layout.TextAreaHeightDots - 1);
    }

    [Fact]
    public void PhysicalTapePreview_HeightRoundsTwelveMillimetersToEightyFiveDots()
    {
        Assert.Equal(85, LabelRasterPreviewConverter.PhysicalTapeHeightDots);
        Assert.Equal(
            12.0,
            PrinterLengthConverter.DotsToMillimeters(
                LabelRasterPreviewConverter.PhysicalTapeHeightDots),
            precision: 1);
    }

    [Fact]
    public void PhysicalTapePreview_CropIsCenteredAroundPrintableBand()
    {
        int topMargin = LabelRasterPreviewConverter.PhysicalTapeTopHeadDot;
        int bottomMargin = LabelRaster.HeadDotCount
            - topMargin
            - LabelRasterPreviewConverter.PhysicalTapeHeightDots;
        TextLabelLayout layout = TextLabelLayout.TwelveMillimeter;

        Assert.Equal(21, topMargin);
        Assert.Equal(22, bottomMargin);
        Assert.Equal(11, layout.PrintableTopDot - topMargin);
        Assert.Equal(
            10,
            topMargin
                + LabelRasterPreviewConverter.PhysicalTapeHeightDots
                - layout.PrintableTopDot
                - layout.PrintableHeightDots);
    }

    [Fact]
    public void PreviewConversion_CropsToPhysicalTapeAndPreservesMappedRasterPixels()
    {
        int topHeadDot = LabelRasterPreviewConverter.PhysicalTapeTopHeadDot;
        int bottomHeadDot = topHeadDot + LabelRasterPreviewConverter.PhysicalTapeHeightDots - 1;
        bool[,] pixels = new bool[4, LabelRaster.HeadDotCount];
        pixels[0, topHeadDot] = true;
        pixels[1, topHeadDot + 42] = true;
        pixels[2, bottomHeadDot] = true;
        pixels[3, topHeadDot - 1] = true;
        pixels[3, bottomHeadDot + 1] = true;
        LabelRaster raster = LabelRaster.Pack(pixels);

        BitmapSource preview = LabelRasterPreviewConverter.ToBitmapSource(raster);
        int stride = preview.PixelWidth;
        byte[] previewPixels = new byte[stride * preview.PixelHeight];
        preview.CopyPixels(previewPixels, stride, 0);

        Assert.Equal(4, preview.PixelWidth);
        Assert.Equal(85, preview.PixelHeight);

        for (int tapeDot = 0; tapeDot < preview.PixelHeight; tapeDot++)
        {
            for (int line = 0; line < preview.PixelWidth; line++)
            {
                int sourceHeadDot = topHeadDot + tapeDot;
                byte expected = raster.IsBlackPixel(line, sourceHeadDot) ? (byte)0x00 : (byte)0xFF;
                Assert.Equal(expected, previewPixels[(tapeDot * stride) + line]);
            }
        }

        Assert.All(
            Enumerable.Range(0, preview.PixelHeight),
            tapeDot => Assert.Equal(0xFF, previewPixels[(tapeDot * stride) + 3]));
    }

    [Fact]
    public void PhysicalTapePreview_AspectRatioUsesLabelLengthAndTwelveMillimeterHeight()
    {
        LabelRaster raster = new(
            rasterLineCount: 180,
            new byte[180 * LabelRaster.BytesPerRasterLine]);

        BitmapSource preview = LabelRasterPreviewConverter.ToBitmapSource(raster);
        double expectedPhysicalAspectRatio = raster.LengthMillimeters
            / LabelRasterPreviewConverter.PhysicalTapeHeightMillimeters;
        double previewPixelAspectRatio = preview.PixelWidth / (double)preview.PixelHeight;

        Assert.InRange(
            Math.Abs(previewPixelAspectRatio - expectedPhysicalAspectRatio),
            0,
            0.002);
    }

    private static TextLabelRenderResult RenderOnStaThread(
        string text,
        TextLabelLayout? layout = null,
        Typeface? typeface = null,
        double? requestedFontSizeDots = null)
    {
        return RunOnStaThread(() => TextLabelRasterizer.Render(
            text,
            typeface ?? new Typeface("Segoe UI"),
            layout ?? TextLabelLayout.TwelveMillimeter,
            requestedFontSizeDots));
    }

    private static T RunOnStaThread<T>(Func<T> action)
    {
        object? result = null;
        Exception? renderingException = null;

        Thread thread = new(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                renderingException = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (renderingException is not null)
        {
            ExceptionDispatchInfo.Capture(renderingException).Throw();
        }

        return result is T typedResult
            ? typedResult
            : throw new InvalidOperationException("The rendering operation returned no result.");
    }
}
