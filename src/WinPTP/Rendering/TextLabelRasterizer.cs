using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinPTP.Rendering;

internal sealed record TextLabelRenderResult(LabelRaster Raster, double SelectedFontSizeDots);

internal static class TextLabelRasterizer
{
    private const double MinimumFontSizeDots = 1;
    private const double FontSizeStepDots = 0.5;
    private const byte BlackThreshold = 160;

    public static TextLabelRenderResult Render(
        string text,
        Typeface typeface,
        TextLabelLayout layout,
        double? requestedFontSizeDots = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentNullException.ThrowIfNull(typeface);
        ArgumentNullException.ThrowIfNull(layout);

        if (requestedFontSizeDots is double requestedFontSize
            && (requestedFontSize < MinimumFontSizeDots || !double.IsFinite(requestedFontSize)))
        {
            throw new ArgumentOutOfRangeException(nameof(requestedFontSizeDots));
        }

        (FormattedText largestFittingText, double largestFittingFontSize) = CreateLargestFittingText(
            text,
            typeface,
            layout.TextAreaHeightDots);
        (FormattedText formattedText, double fontSize) = SelectTextSize(
            text,
            typeface,
            layout.TextAreaHeightDots,
            requestedFontSizeDots,
            largestFittingText,
            largestFittingFontSize);

        int labelLengthDots = Math.Max(
            1,
            (int)Math.Ceiling(formattedText.WidthIncludingTrailingWhitespace)
                + (layout.HorizontalPaddingDots * 2));
        double textTop = layout.TextAreaTopDot
            + ((layout.TextAreaHeightDots - formattedText.Height) / 2.0);

        DrawingVisual visual = new();
        TextOptions.SetTextFormattingMode(visual, TextFormattingMode.Display);
        TextOptions.SetTextRenderingMode(visual, TextRenderingMode.Grayscale);

        using (DrawingContext drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(
                Brushes.White,
                null,
                new Rect(0, 0, labelLengthDots, LabelRaster.HeadDotCount));
            drawing.DrawText(formattedText, new Point(layout.HorizontalPaddingDots, textTop));
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

        return new TextLabelRenderResult(LabelRaster.Pack(blackPixels), fontSize);
    }

    private static (FormattedText Text, double FontSize) SelectTextSize(
        string text,
        Typeface typeface,
        int maximumHeightDots,
        double? requestedFontSizeDots,
        FormattedText largestFittingText,
        double largestFittingFontSize)
    {
        if (requestedFontSizeDots is not double requestedFontSize)
        {
            return (largestFittingText, largestFittingFontSize);
        }

        FormattedText requestedText = CreateFormattedText(text, typeface, requestedFontSize);
        return requestedText.Height <= maximumHeightDots
            ? (requestedText, requestedFontSize)
            : (largestFittingText, largestFittingFontSize);
    }

    private static (FormattedText Text, double FontSize) CreateLargestFittingText(
        string text,
        Typeface typeface,
        int maximumHeightDots)
    {
        int minimumStep = (int)(MinimumFontSizeDots / FontSizeStepDots);
        int maximumStep = (int)(maximumHeightDots / FontSizeStepDots);
        int selectedStep = -1;
        FormattedText? largestText = null;

        while (minimumStep <= maximumStep)
        {
            int candidateStep = minimumStep + ((maximumStep - minimumStep) / 2);
            double fontSize = candidateStep * FontSizeStepDots;
            FormattedText candidate = CreateFormattedText(text, typeface, fontSize);
            if (candidate.Height <= maximumHeightDots)
            {
                selectedStep = candidateStep;
                largestText = candidate;
                minimumStep = candidateStep + 1;
            }
            else
            {
                maximumStep = candidateStep - 1;
            }
        }

        if (largestText is null)
        {
            throw new InvalidOperationException("The text cannot fit within the configured printable height.");
        }

        return (largestText, selectedStep * FontSizeStepDots);
    }

    private static FormattedText CreateFormattedText(string text, Typeface typeface, double fontSize)
    {
        return new FormattedText(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            typeface,
            fontSize,
            Brushes.Black,
            1.0);
    }
}
