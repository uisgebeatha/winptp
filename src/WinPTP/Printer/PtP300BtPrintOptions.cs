using WinPTP.Rendering;

namespace WinPTP.Printer;

internal sealed record PtP300BtPrintOptions
{
    public const int MinimumCopies = LabelRasterComposer.MinimumCopies;
    public const int MaximumCopies = LabelRasterComposer.MaximumCopies;

    public PtP300BtPrintOptions(int copies)
    {
        if (copies is < MinimumCopies or > MaximumCopies)
        {
            throw new ArgumentOutOfRangeException(
                nameof(copies),
                $"Copies must be between {MinimumCopies} and {MaximumCopies}.");
        }

        Copies = copies;
    }

    public int Copies { get; }
}
