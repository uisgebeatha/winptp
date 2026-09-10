namespace WinPTP.Rendering;

internal sealed class TextLabelLayout
{
    public static TextLabelLayout TwelveMillimeter { get; } = new(
        printableTopDot: 32,
        printableHeightDots: 64,
        verticalSafetyMarginDots: 2,
        horizontalPaddingDots: 12);

    public TextLabelLayout(
        int printableTopDot,
        int printableHeightDots,
        int verticalSafetyMarginDots,
        int horizontalPaddingDots)
    {
        if (printableTopDot < 0 || printableHeightDots <= 0
            || printableTopDot + printableHeightDots > LabelRaster.HeadDotCount)
        {
            throw new ArgumentOutOfRangeException(nameof(printableHeightDots));
        }

        if (verticalSafetyMarginDots < 0
            || verticalSafetyMarginDots * 2 >= printableHeightDots)
        {
            throw new ArgumentOutOfRangeException(nameof(verticalSafetyMarginDots));
        }

        ArgumentOutOfRangeException.ThrowIfNegative(horizontalPaddingDots);

        PrintableTopDot = printableTopDot;
        PrintableHeightDots = printableHeightDots;
        VerticalSafetyMarginDots = verticalSafetyMarginDots;
        HorizontalPaddingDots = horizontalPaddingDots;
    }

    public int PrintableTopDot { get; }

    public int PrintableHeightDots { get; }

    public int VerticalSafetyMarginDots { get; }

    public int HorizontalPaddingDots { get; }

    public int TextAreaTopDot => PrintableTopDot + VerticalSafetyMarginDots;

    public int TextAreaHeightDots => PrintableHeightDots - (VerticalSafetyMarginDots * 2);
}
