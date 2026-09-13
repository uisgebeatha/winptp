using WinPTP.Rendering;

namespace WinPTP.Printer;

internal sealed record PrintTapeUseEstimate(
    int ContentDotsPerCopy,
    int Copies,
    int SeparatorDotsPerGap,
    int FinalFeedMarginDots)
{
    public int TotalContentDots => checked(ContentDotsPerCopy * Copies);

    public int SeparatorCount => Copies - 1;

    public int TotalSeparatorDots => checked(SeparatorDotsPerGap * SeparatorCount);

    public int CompositeRasterDots => LabelRasterComposer.CalculateRasterLineCount(
        ContentDotsPerCopy,
        Copies,
        SeparatorDotsPerGap);

    public int EstimatedCommandedDots => checked(CompositeRasterDots + FinalFeedMarginDots);

    public double ContentMillimetersPerCopy =>
        PrinterLengthConverter.DotsToMillimeters(ContentDotsPerCopy);

    public double EstimatedCommandedMillimeters =>
        PrinterLengthConverter.DotsToMillimeters(EstimatedCommandedDots);
}

internal static class PrintTapeUseCalculator
{
    public static PrintTapeUseEstimate Calculate(
        LabelRaster raster,
        PtP300BtPrintOptions options)
    {
        ArgumentNullException.ThrowIfNull(raster);
        ArgumentNullException.ThrowIfNull(options);

        return new PrintTapeUseEstimate(
            raster.RasterLineCount,
            options.Copies,
            LabelRasterComposer.CopySeparatorDots,
            PtP300BtPrintJobBuilder.EndFeedMarginDots);
    }
}
