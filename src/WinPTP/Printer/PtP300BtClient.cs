using System.Diagnostics;
using System.IO.Ports;
using WinPTP.Rendering;

namespace WinPTP.Printer;

internal static class PtP300BtClient
{
    private const int ResponseLength = PtP300BtStatusParser.ResponseLength;
    private const int SerialTimeoutMilliseconds = 3000;
    private const int PostPrintCompletionTimeoutMilliseconds = 10000;

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

        using SerialPort port = CreateSerialPort(portName);

        port.Open();
        return QueryStatus(port);
    }

    public static PtP300BtPrintResult Print(string portName, LabelRaster raster)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portName);
        ArgumentNullException.ThrowIfNull(raster);

        using SerialPort port = CreateSerialPort(portName);
        port.Open();

        PtP300BtStatus initialStatus = QueryStatus(port);
        if (!initialStatus.IsReady)
        {
            throw new PtP300BtPrintException($"Printer is not ready: {initialStatus.StateDescription}.");
        }

        if (initialStatus.TapeWidthMillimeters != 12)
        {
            throw new PtP300BtPrintException(
                $"Printing currently supports 12 mm tape only; detected {initialStatus.TapeWidthMillimeters} mm.");
        }

        byte[] printJob = PtP300BtPrintJobBuilder.Build(initialStatus, raster);
        port.Write(printJob, 0, printJob.Length);

        PtP300BtStatus? finalStatus = ReadPostPrintStatus(port);
        return new PtP300BtPrintResult(finalStatus);
    }

    private static SerialPort CreateSerialPort(string portName)
    {
        return new SerialPort(portName, 9600, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            ReadTimeout = SerialTimeoutMilliseconds,
            WriteTimeout = SerialTimeoutMilliseconds
        };
    }

    private static PtP300BtStatus QueryStatus(SerialPort port)
    {
        port.DiscardInBuffer();
        port.DiscardOutBuffer();
        port.Write(StatusRequest, 0, StatusRequest.Length);

        byte[] response = ReadExact(
            port,
            ResponseLength,
            Stopwatch.StartNew(),
            SerialTimeoutMilliseconds)!;
        return PtP300BtStatusParser.Parse(response);
    }

    private static PtP300BtStatus? ReadPostPrintStatus(SerialPort port)
    {
        Stopwatch timeout = Stopwatch.StartNew();
        PtP300BtStatus? latestStatus = null;

        while (timeout.ElapsedMilliseconds < PostPrintCompletionTimeoutMilliseconds)
        {
            byte[]? response = ReadExact(
                port,
                ResponseLength,
                timeout,
                PostPrintCompletionTimeoutMilliseconds,
                optional: true);
            if (response is null)
            {
                return latestStatus;
            }

            PtP300BtStatus status = PtP300BtStatusParser.Parse(response);
            latestStatus = status;
            if (status.ErrorFlags != 0 || status.IsReady || status.StatusType == 0x01)
            {
                return status;
            }
        }

        return latestStatus;
    }

    private static byte[]? ReadExact(
        SerialPort port,
        int count,
        Stopwatch timeout,
        int timeoutMilliseconds,
        bool optional = false)
    {
        byte[] response = new byte[count];
        int bytesRead = 0;

        while (bytesRead < count)
        {
            int remainingMilliseconds = timeoutMilliseconds - (int)timeout.ElapsedMilliseconds;
            if (remainingMilliseconds <= 0)
            {
                if (optional && bytesRead == 0)
                {
                    return null;
                }

                throw CreateIncompleteResponseException(bytesRead, timeoutMilliseconds);
            }

            port.ReadTimeout = remainingMilliseconds;

            try
            {
                int read = port.Read(response, bytesRead, count - bytesRead);
                if (read == 0)
                {
                    throw CreateIncompleteResponseException(bytesRead, timeoutMilliseconds);
                }

                bytesRead += read;
            }
            catch (TimeoutException)
            {
                if (optional && bytesRead == 0)
                {
                    return null;
                }

                throw CreateIncompleteResponseException(bytesRead, timeoutMilliseconds);
            }
        }

        return response;
    }

    private static TimeoutException CreateIncompleteResponseException(int bytesRead, int timeoutMilliseconds)
    {
        return new TimeoutException(
            $"Received {bytesRead} of the expected {ResponseLength} status bytes within {timeoutMilliseconds / 1000} seconds.");
    }
}
