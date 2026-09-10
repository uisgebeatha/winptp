namespace WinPTP.Rendering;

internal sealed class LabelRaster
{
    public const int HeadDotCount = 128;
    public const int BytesPerRasterLine = HeadDotCount / 8;

    private readonly byte[] _packedData;

    public LabelRaster(int rasterLineCount, ReadOnlySpan<byte> packedData)
    {
        if (rasterLineCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rasterLineCount));
        }

        if (packedData.Length != rasterLineCount * BytesPerRasterLine)
        {
            throw new ArgumentException(
                $"Expected {rasterLineCount * BytesPerRasterLine} packed raster bytes.",
                nameof(packedData));
        }

        RasterLineCount = rasterLineCount;
        _packedData = packedData.ToArray();
    }

    public int RasterLineCount { get; }

    public int DotsPerRasterLine => HeadDotCount;

    public ReadOnlyMemory<byte> PackedData => _packedData;

    public ReadOnlySpan<byte> GetRasterLine(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        if (index >= RasterLineCount)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        return _packedData.AsSpan(index * BytesPerRasterLine, BytesPerRasterLine);
    }

    public static LabelRaster Pack(bool[,] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);

        int rasterLineCount = pixels.GetLength(0);
        if (rasterLineCount <= 0)
        {
            throw new ArgumentException("At least one raster line is required.", nameof(pixels));
        }

        if (pixels.GetLength(1) != HeadDotCount)
        {
            throw new ArgumentException(
                $"Each raster line must contain exactly {HeadDotCount} head dots.",
                nameof(pixels));
        }

        byte[] packedData = new byte[rasterLineCount * BytesPerRasterLine];

        for (int line = 0; line < rasterLineCount; line++)
        {
            for (int headDot = 0; headDot < HeadDotCount; headDot++)
            {
                if (pixels[line, headDot])
                {
                    int byteOffset = (line * BytesPerRasterLine) + (headDot / 8);
                    packedData[byteOffset] |= (byte)(0x80 >> (headDot % 8));
                }
            }
        }

        return new LabelRaster(rasterLineCount, packedData);
    }
}
