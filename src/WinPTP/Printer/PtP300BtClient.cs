using System.Diagnostics;
using System.IO.Ports;

namespace WinPTP.Printer;

internal static class PtP300BtClient
{
    private const int ResponseLength = PtP300BtStatusParser.ResponseLength;
    private const int TimeoutMilliseconds = 3000;

    private static readonly byte[] StatusRequest =
    [
        .. new byte[64],
        0x1B, 0x40,
        0x1B, 0x69, 0x61, 0x01,
        0x1B, 0x69, 0x53
    ];

    public static PtP300BtStatus QueryStatus(string portName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);

        using SerialPort port = new(portName, 9600, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            ReadTimeout = TimeoutMilliseconds,
            WriteTimeout = TimeoutMilliseconds
        };

        port.Open();
        port.DiscardInBuffer();
        port.DiscardOutBuffer();
        port.Write(StatusRequest, 0, StatusRequest.Length);

        byte[] response = ReadExact(port, ResponseLength);
        return PtP300BtStatusParser.Parse(response);
    }

    private static byte[] ReadExact(SerialPort port, int count)
    {
        byte[] response = new byte[count];
        int bytesRead = 0;
        Stopwatch stopwatch = Stopwatch.StartNew();

        while (bytesRead < count)
        {
            int remainingMilliseconds = TimeoutMilliseconds - (int)stopwatch.ElapsedMilliseconds;
            if (remainingMilliseconds <= 0)
            {
                throw CreateIncompleteResponseException(bytesRead);
            }

            port.ReadTimeout = remainingMilliseconds;

            try
            {
                int read = port.Read(response, bytesRead, count - bytesRead);
                if (read == 0)
                {
                    throw CreateIncompleteResponseException(bytesRead);
                }

                bytesRead += read;
            }
            catch (TimeoutException)
            {
                throw CreateIncompleteResponseException(bytesRead);
            }
        }

        return response;
    }

    private static TimeoutException CreateIncompleteResponseException(int bytesRead)
    {
        return new TimeoutException(
            $"Received {bytesRead} of the expected {ResponseLength} status bytes within {TimeoutMilliseconds / 1000} seconds.");
    }
}
