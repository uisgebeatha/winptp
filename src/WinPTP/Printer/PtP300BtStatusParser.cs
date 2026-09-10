namespace WinPTP.Printer;

internal static class PtP300BtStatusParser
{
    public const int ResponseLength = 32;

    private const byte PtP300BtModel = 0x72;

    private static readonly byte[] StatusMagic = [0x80, 0x20, 0x42, 0x30];

    public static PtP300BtStatus Parse(ReadOnlySpan<byte> response)
    {
        if (response.Length != ResponseLength)
        {
            throw new PtP300BtResponseException(
                $"The status response was {response.Length} bytes; expected {ResponseLength}.");
        }

        if (!response[..StatusMagic.Length].SequenceEqual(StatusMagic))
        {
            throw new PtP300BtResponseException("The device returned an invalid printer status header.");
        }

        if (response[4] != PtP300BtModel)
        {
            throw new PtP300BtResponseException(
                $"The responding device is not a PT-P300BT (model byte 0x{response[4]:X2}).");
        }

        return new PtP300BtStatus(
            ErrorFlags: ReadBigEndianUInt16(response, 8),
            TapeWidthMillimeters: response[10],
            MediaType: response[11],
            FixedTapeLength: response[17],
            StatusType: response[18],
            PhaseType: response[19],
            Phase: ReadBigEndianUInt16(response, 20));
    }

    private static ushort ReadBigEndianUInt16(ReadOnlySpan<byte> response, int offset)
    {
        return (ushort)((response[offset] << 8) | response[offset + 1]);
    }
}
