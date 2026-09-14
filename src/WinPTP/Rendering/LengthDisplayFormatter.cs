using System.Globalization;

namespace WinPTP.Rendering;

internal static class LengthDisplayFormatter
{
    public static string FormatMillimeters(double millimeters) =>
        Math.Ceiling(millimeters).ToString("F0", CultureInfo.CurrentCulture) + " mm";
}
