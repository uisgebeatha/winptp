using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinPTP.Rendering;

internal static class LabelRasterPreviewConverter
{
    public static BitmapSource ToBitmapSource(LabelRaster raster)
    {
        ArgumentNullException.ThrowIfNull(raster);

        int width = raster.RasterLineCount;
        int height = raster.DotsPerRasterLine;
        int stride = width;
        byte[] pixels = new byte[stride * height];

        for (int headDot = 0; headDot < height; headDot++)
        {
            for (int line = 0; line < width; line++)
            {
                pixels[(headDot * stride) + line] = raster.IsBlackPixel(line, headDot)
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
