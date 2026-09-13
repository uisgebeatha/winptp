namespace WinPTP.Rendering;

internal static class LabelRasterComposer
{
    public const int MinimumCopies = 1;
    public const int MaximumCopies = 99;
    public const int CopySeparatorDots = 28;

    public static LabelRaster Compose(
        LabelRaster original,
        int copies,
        int separatorDots = CopySeparatorDots)
    {
        ArgumentNullException.ThrowIfNull(original);
        Validate(copies, separatorDots);

        if (copies == 1)
        {
            return original;
        }

        int rasterLineCount = CalculateRasterLineCount(
            original.RasterLineCount,
            copies,
            separatorDots);
        byte[] packedData = new byte[checked(rasterLineCount * LabelRaster.BytesPerRasterLine)];
        int destinationOffset = 0;

        for (int copy = 0; copy < copies; copy++)
        {
            original.PackedData.Span.CopyTo(packedData.AsSpan(destinationOffset));
            destinationOffset += original.PackedData.Length;

            if (copy < copies - 1)
            {
                destinationOffset += checked(separatorDots * LabelRaster.BytesPerRasterLine);
            }
        }

        return new LabelRaster(rasterLineCount, packedData);
    }

    public static int CalculateRasterLineCount(
        int originalRasterLineCount,
        int copies,
        int separatorDots = CopySeparatorDots)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(originalRasterLineCount);
        Validate(copies, separatorDots);

        return checked(
            (copies * originalRasterLineCount)
            + ((copies - 1) * separatorDots));
    }

    private static void Validate(int copies, int separatorDots)
    {
        if (copies is < MinimumCopies or > MaximumCopies)
        {
            throw new ArgumentOutOfRangeException(
                nameof(copies),
                $"Copies must be between {MinimumCopies} and {MaximumCopies}.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(separatorDots);
    }
}
