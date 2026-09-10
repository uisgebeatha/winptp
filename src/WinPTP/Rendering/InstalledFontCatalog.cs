using System.Windows;
using System.Windows.Media;

namespace WinPTP.Rendering;

internal sealed record InstalledFontFamily(string DisplayName, FontFamily FontFamily)
{
    public Typeface CreateTypeface() => new(
        FontFamily,
        FontStyles.Normal,
        FontWeights.Normal,
        FontStretches.Normal);
}

internal static class InstalledFontCatalog
{
    private static readonly Lazy<IReadOnlyList<InstalledFontFamily>> InstalledFamilies =
        new(LoadInstalledFamilies);

    public static IReadOnlyList<InstalledFontFamily> GetInstalledFamilies() =>
        InstalledFamilies.Value;

    private static IReadOnlyList<InstalledFontFamily> LoadInstalledFamilies()
    {
        return Fonts.SystemFontFamilies
            .Where(family => !string.IsNullOrWhiteSpace(family.Source))
            .GroupBy(family => family.Source, StringComparer.CurrentCultureIgnoreCase)
            .Select(group => new InstalledFontFamily(group.Key, group.First()))
            .OrderBy(family => family.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }
}
