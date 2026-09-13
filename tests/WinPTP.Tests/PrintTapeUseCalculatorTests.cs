using WinPTP.Printer;
using WinPTP.Rendering;

namespace WinPTP.Tests;

public sealed class PrintTapeUseCalculatorTests
{
    [Fact]
    public void Calculate_OneCopyIncludesContentAndOneFeedMargin()
    {
        LabelRaster raster = CreateRaster(rasterLineCount: 100);

        PrintTapeUseEstimate estimate = PrintTapeUseCalculator.Calculate(
            raster,
            new PtP300BtPrintOptions(copies: 1));

        Assert.Equal(100, estimate.ContentDotsPerCopy);
        Assert.Equal(100, estimate.TotalContentDots);
        Assert.Equal(0, estimate.SeparatorCount);
        Assert.Equal(0, estimate.TotalSeparatorDots);
        Assert.Equal(100, estimate.CompositeRasterDots);
        Assert.Equal(28, estimate.FinalFeedMarginDots);
        Assert.Equal(128, estimate.EstimatedCommandedDots);
        Assert.Equal(
            PrinterLengthConverter.DotsToMillimeters(128),
            estimate.EstimatedCommandedMillimeters,
            precision: 10);
    }

    [Fact]
    public void Calculate_TwoCopiesIncludesOneSeparatorAndOneFinalFeedMargin()
    {
        PrintTapeUseEstimate estimate = PrintTapeUseCalculator.Calculate(
            CreateRaster(rasterLineCount: 100),
            new PtP300BtPrintOptions(copies: 2));

        Assert.Equal(200, estimate.TotalContentDots);
        Assert.Equal(1, estimate.SeparatorCount);
        Assert.Equal(28, estimate.TotalSeparatorDots);
        Assert.Equal(228, estimate.CompositeRasterDots);
        Assert.Equal(28, estimate.FinalFeedMarginDots);
        Assert.Equal(256, estimate.EstimatedCommandedDots);
    }

    [Fact]
    public void Calculate_NCopiesIncludesNMinusOneSeparatorsAndOneFinalFeedMargin()
    {
        PrintTapeUseEstimate estimate = PrintTapeUseCalculator.Calculate(
            CreateRaster(rasterLineCount: 100),
            new PtP300BtPrintOptions(copies: 3));

        Assert.Equal(300, estimate.TotalContentDots);
        Assert.Equal(2, estimate.SeparatorCount);
        Assert.Equal(56, estimate.TotalSeparatorDots);
        Assert.Equal(356, estimate.CompositeRasterDots);
        Assert.Equal(28, estimate.FinalFeedMarginDots);
        Assert.Equal(384, estimate.EstimatedCommandedDots);
    }

    private static LabelRaster CreateRaster(int rasterLineCount)
    {
        return new LabelRaster(
            rasterLineCount,
            new byte[rasterLineCount * LabelRaster.BytesPerRasterLine]);
    }
}
