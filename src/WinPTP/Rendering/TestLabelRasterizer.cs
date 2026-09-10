using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinPTP.Rendering;

internal static class TestLabelRasterizer
{
    public const string LabelText = "WinPTP Test";

    private const int SafePrintableTopDot = 32;
    private const int SafePrintableHeightDots = 64;
    private const int HorizontalPaddingDots = 12;
    private const double FontSizeDots = 36;
    private const byte BlackThreshold = 160;

    public static LabelRaster Render()
    {
        FormattedText text = new(
            LabelText,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            FontSizeDots,
            Brushes.Black,
            1.0);

        int labelLengthDots = (int)Math.Ceiling(text.WidthIncludingTrailingWhitespace)
            + (HorizontalPaddingDots * 2);
        double textTop = SafePrintableTopDot + ((SafePrintableHeightDots - text.Height) / 2.0);

        DrawingVisual visual = new();
        TextOptions.SetTextFormattingMode(visual, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Grayscale);

        using (DrawingContext drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(
                Brushes.White,
                null,
                new Rect(0, 0, labelLengthDots, LabelRaster.HeadDotCount));
            drawing.DrawText(text, new Point(HorizontalPaddingDots, textTop));
        }

        RenderTargetBitmap bitmap = new(
            labelLengthDots,
            LabelRaster.HeadDotCount,
            96,
            96,
            PixelFormats.Pbgra32);
        bitmap.Render(visual);

        int stride = labelLengthDots * 4;
        byte[] pixels = new byte[stride * LabelRaster.HeadDotCount];
        bitmap.CopyPixels(pixels, stride, 0);

        bool[,] blackPixels = new bool[labelLengthDots, LabelRaster.HeadDotCount];
        for (int headDot = 0; headDot < LabelRaster.HeadDotCount; headDot++)
        {
            for (int line = 0; line < labelLengthDots; line++)
            {
                int pixelOffset = (headDot * stride) + (line * 4);
                int luminance = (pixels[pixelOffset] + pixels[pixelOffset + 1] + pixels[pixelOffset + 2]) / 3;
                blackPixels[line, headDot] = luminance < BlackThreshold;
            }
        }

        return LabelRaster.Pack(blackPixels);
    }
}
