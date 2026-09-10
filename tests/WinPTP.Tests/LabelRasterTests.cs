using System.Runtime.ExceptionServices;
using WinPTP.Rendering;

namespace WinPTP.Tests;

public sealed class LabelRasterTests
{
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
    public void Render_WinPtpTest_HasPrinterDimensionsAndBlankAndPrintedPixels()
    {
        LabelRaster raster = RenderTestLabelOnStaThread();
        byte[] packedData = raster.PackedData.ToArray();

        Assert.Equal("WinPTP Test", TestLabelRasterizer.LabelText);
        Assert.Equal(128, raster.DotsPerRasterLine);
        Assert.Equal(16, LabelRaster.BytesPerRasterLine);
        Assert.True(raster.RasterLineCount > 0);
        Assert.Equal(raster.RasterLineCount * LabelRaster.BytesPerRasterLine, packedData.Length);
        Assert.Equal(LabelRaster.BytesPerRasterLine, raster.GetRasterLine(0).Length);
        Assert.Contains(packedData, value => value == 0x00);
        Assert.Contains(packedData, value => value != 0x00);
    }

    private static LabelRaster RenderTestLabelOnStaThread()
    {
        LabelRaster? raster = null;
        Exception? renderingException = null;

        Thread thread = new(() =>
        {
            try
            {
                raster = TestLabelRasterizer.Render();
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

        return raster ?? throw new InvalidOperationException("The test label renderer returned no raster.");
    }
}
