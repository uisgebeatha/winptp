namespace WinPTP.Printer;

internal enum PtP300BtPrintOutcome
{
    PrintingCompleted,
    Printing,
    PrinterError,
    CompletionStatusNotReceived,
    OtherStatusReceived
}

internal sealed record PtP300BtPrintResult(PtP300BtStatus? LastStatus)
{
    public PtP300BtPrintOutcome Outcome
    {
        get
        {
            if (LastStatus is null)
            {
                return PtP300BtPrintOutcome.CompletionStatusNotReceived;
            }

            if (LastStatus.ErrorFlags != 0)
            {
                return PtP300BtPrintOutcome.PrinterError;
            }

            if (LastStatus.StatusType == 0x01 || LastStatus.IsReady)
            {
                return PtP300BtPrintOutcome.PrintingCompleted;
            }

            if (LastStatus.StatusType == 0x06
                && LastStatus.PhaseType == 0x01
                && LastStatus.Phase == 0x0000)
            {
                return PtP300BtPrintOutcome.Printing;
            }

            return PtP300BtPrintOutcome.OtherStatusReceived;
        }
    }

    public string Description => Outcome switch
    {
        PtP300BtPrintOutcome.PrintingCompleted => "Printing completed",
        PtP300BtPrintOutcome.Printing => "Printing / print in progress",
        PtP300BtPrintOutcome.PrinterError => $"Printer error (flags 0x{LastStatus!.ErrorFlags:X4})",
        PtP300BtPrintOutcome.CompletionStatusNotReceived => "Print command sent but completion status not received",
        _ => $"Printer status received (status type 0x{LastStatus!.StatusType:X2}, "
            + $"phase type 0x{LastStatus.PhaseType:X2}, phase 0x{LastStatus.Phase:X4})"
    };
}
