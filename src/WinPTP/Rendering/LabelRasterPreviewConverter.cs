using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinPTP.Rendering;

internal static class LabelRasterPreviewConverter
{
    public const double PhysicalTapeHeightMillimeters = 12.0;

    public static int PhysicalTapeHeightDots { get; } = (int)Math.Round(
        PhysicalTapeHeightMillimeters * PrinterLengthConverter.PrinterDotsPerInch / 25.4);

    public static int PhysicalTapeTopHeadDot =>
        (LabelRaster.HeadDotCount - PhysicalTapeHeightDots) / 2;

    public static BitmapSource ToBitmapSource(LabelRaster raster)
    {
        ArgumentNullException.ThrowIfNull(raster);

        int width = raster.RasterLineCount;
        int height = PhysicalTapeHeightDots;
        int stride = width;
        byte[] pixels = new byte[stride * height];

        for (int tapeDot = 0; tapeDot < height; tapeDot++)
        {
            int headDot = PhysicalTapeTopHeadDot + tapeDot;
            for (int line = 0; line < width; line++)
            {
                pixels[(tapeDot * stride) + line] = raster.IsBlackPixel(line, headDot)
                    ? (byte)0x00
                    : (byte)0xFF;
            }
        }

        BitmapSource preview = BitmapSource.Create(
            width,
            height,
            96,
            96,
            PixelFormats.Gray8,
            null,
            pixels,
            stride);
        preview.Freeze();
        return preview;
    }
}
