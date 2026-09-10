namespace WinPTP.Printer;

internal sealed record PtP300BtStatus(
    ushort ErrorFlags,
    byte TapeWidthMillimeters,
    byte MediaType,
    byte FixedTapeLength,
    byte StatusType,
    byte PhaseType,
    ushort Phase)
{
    public bool IsReady => ErrorFlags == 0 && PhaseType == 0 && Phase == 0;

    public string MediaTypeDescription => MediaType switch
    {
        0x00 => "Not loaded",
        0x01 => "Laminated TZe",
        0x03 => "Non-laminated TZeN",
        0x11 => "Heat-shrink",
        0x4A => "Continuous tape",
        0x4B => "Die-cut labels",
        0xFF => "Unsupported",
        _ => "Unknown"
    };

    public string StateDescription
    {
        get
        {
            if (ErrorFlags != 0)
            {
                return $"Error (printer flags 0x{ErrorFlags:X4})";
            }

            if (IsReady)
            {
                return "Ready";
            }

            return $"Not ready (status type 0x{StatusType:X2}, phase type 0x{PhaseType:X2}, phase 0x{Phase:X4})";
        }
    }
}
