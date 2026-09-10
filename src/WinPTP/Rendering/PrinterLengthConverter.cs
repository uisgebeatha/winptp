namespace WinPTP.Rendering;

internal static class PrinterLengthConverter
{
    public const double PrinterDotsPerInch = 180.0;

    public static double DotsToMillimeters(int rasterDots)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(rasterDots);
        return rasterDots * 25.4 / PrinterDotsPerInch;
    }
}
