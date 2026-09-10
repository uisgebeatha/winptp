using System.Runtime.ExceptionServices;
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
    public void PreviewConversion_PreservesRasterPixelPositionsAndOrientation()
    {
        bool[,] pixels = new bool[3, LabelRaster.HeadDotCount];
        pixels[0, 0] = true;
        pixels[1, 7] = true;
        pixels[2, 127] = true;
        LabelRaster raster = LabelRaster.Pack(pixels);

        BitmapSource preview = LabelRasterPreviewConverter.ToBitmapSource(raster);
        int stride = preview.PixelWidth;
        byte[] previewPixels = new byte[stride * preview.PixelHeight];
        preview.CopyPixels(previewPixels, stride, 0);

        Assert.Equal(3, preview.PixelWidth);
        Assert.Equal(128, preview.PixelHeight);

        for (int headDot = 0; headDot < preview.PixelHeight; headDot++)
        {
            for (int line = 0; line < preview.PixelWidth; line++)
            {
                byte expected = raster.IsBlackPixel(line, headDot) ? (byte)0x00 : (byte)0xFF;
                Assert.Equal(expected, previewPixels[(headDot * stride) + line]);
            }
        }
    }

    private static TextLabelRenderResult RenderOnStaThread(
        string text,
        TextLabelLayout? layout = null)
    {
        return RunOnStaThread(() => TextLabelRasterizer.Render(
            text,
            new Typeface("Segoe UI"),
            layout ?? TextLabelLayout.TwelveMillimeter));
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
