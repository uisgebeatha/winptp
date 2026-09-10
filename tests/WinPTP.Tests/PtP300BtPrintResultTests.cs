using WinPTP.Printer;

namespace WinPTP.Tests;

public sealed class PtP300BtPrintResultTests
{
    [Fact]
    public void Outcome_PrintingCompletedStatus_IsCompleted()
    {
        PtP300BtStatus status = CreateStatus(statusType: 0x01, phaseType: 0x01, phase: 0x0000);

        PtP300BtPrintResult result = new(status);

        Assert.Equal(PtP300BtPrintOutcome.PrintingCompleted, result.Outcome);
        Assert.Equal("Printing completed", result.Description);
    }

    [Fact]
    public void Outcome_PhaseChangeToPrinting_IsPrintInProgress()
    {
        PtP300BtStatus status = CreateStatus(statusType: 0x06, phaseType: 0x01, phase: 0x0000);

        PtP300BtPrintResult result = new(status);

        Assert.False(status.IsReady);
        Assert.Equal(PtP300BtPrintOutcome.Printing, result.Outcome);
        Assert.Equal("Printing / print in progress", result.Description);
        Assert.DoesNotContain("Not ready", result.Description);
    }

    [Fact]
    public void Outcome_NonZeroErrorFlags_IsPrinterError()
    {
        PtP300BtStatus status = CreateStatus(errorFlags: 0x0020);

        PtP300BtPrintResult result = new(status);

        Assert.Equal(PtP300BtPrintOutcome.PrinterError, result.Outcome);
        Assert.Equal("Printer error (flags 0x0020)", result.Description);
    }

    [Fact]
    public void Outcome_NoStatus_IsCompletionStatusNotReceived()
    {
        PtP300BtPrintResult result = new(null);

        Assert.Equal(PtP300BtPrintOutcome.CompletionStatusNotReceived, result.Outcome);
        Assert.Equal("Print command sent but completion status not received", result.Description);
    }

    private static PtP300BtStatus CreateStatus(
        ushort errorFlags = 0,
        byte statusType = 0,
        byte phaseType = 0,
        ushort phase = 0)
    {
        return new PtP300BtStatus(
            ErrorFlags: errorFlags,
            TapeWidthMillimeters: 12,
            MediaType: 0x01,
            FixedTapeLength: 0,
            StatusType: statusType,
            PhaseType: phaseType,
            Phase: phase);
    }
}
