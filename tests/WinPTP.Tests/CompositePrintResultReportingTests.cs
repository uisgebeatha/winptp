using WinPTP.Printer;

namespace WinPTP.Tests;

public sealed class CompositePrintResultReportingTests
{
    [Fact]
    public void CompletedCompositePrintReportsRequestedCopiesAsOneStrip()
    {
        PtP300BtPrintResult result = new(CreateStatus(statusType: 0x01));

        string message = MainWindow.FormatCompositePrintResult("COM4", copies: 2, result);

        Assert.Equal("Printed 2 copies as one strip on COM4.", message);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedOrUnconfirmedCompositePrintDoesNotClaimCopiesCompleted(bool printerError)
    {
        PtP300BtPrintResult result = printerError
            ? new PtP300BtPrintResult(CreateStatus(errorFlags: 0x0020))
            : new PtP300BtPrintResult(null);

        string message = MainWindow.FormatCompositePrintResult("COM4", copies: 2, result);

        Assert.DoesNotContain("Printed 2 copies", message);
        Assert.DoesNotContain("2 copies completed", message);
    }

    private static PtP300BtStatus CreateStatus(
        ushort errorFlags = 0,
        byte statusType = 0)
    {
        return new PtP300BtStatus(
            ErrorFlags: errorFlags,
            TapeWidthMillimeters: 12,
            MediaType: 0x01,
            FixedTapeLength: 0,
            StatusType: statusType,
            PhaseType: 0,
            Phase: 0);
    }
}
