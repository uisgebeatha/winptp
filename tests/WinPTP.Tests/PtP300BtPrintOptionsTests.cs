using WinPTP.Printer;

namespace WinPTP.Tests;

public sealed class PtP300BtPrintOptionsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100)]
    public void Constructor_InvalidCopiesThrows(int copies)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new PtP300BtPrintOptions(copies));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public void Constructor_ValidCopiesAreAccepted(int copies)
    {
        PtP300BtPrintOptions options = new(copies);

        Assert.Equal(copies, options.Copies);
    }
}
