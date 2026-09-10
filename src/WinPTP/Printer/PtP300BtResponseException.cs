namespace WinPTP.Printer;

internal sealed class PtP300BtResponseException : Exception
{
    public PtP300BtResponseException(string message)
        : base(message)
    {
    }
}
