using System.IO.Ports;
using System.Text.RegularExpressions;

namespace WinPTP.Printer;

internal static partial class SerialPortDiscovery
{
    public static IReadOnlyList<string> GetPortNames()
    {
        return SerialPort.GetPortNames()
            .OrderBy(GetPortNumber)
            .ThenBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int GetPortNumber(string portName)
    {
        Match match = ComPortNumberRegex().Match(portName);
        return match.Success && int.TryParse(match.Groups[1].Value, out int portNumber)
            ? portNumber
            : int.MaxValue;
    }

    [GeneratedRegex("^COM(\\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ComPortNumberRegex();
}
