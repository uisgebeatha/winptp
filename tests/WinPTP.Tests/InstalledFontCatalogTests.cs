using WinPTP.Rendering;

namespace WinPTP.Tests;

public sealed class InstalledFontCatalogTests
{
    [Fact]
    public void GetInstalledFamilies_ReturnsSortedUsableFontFamilies()
    {
        IReadOnlyList<InstalledFontFamily> fonts = InstalledFontCatalog.GetInstalledFamilies();

        Assert.NotEmpty(fonts);
        Assert.All(fonts, font => Assert.False(string.IsNullOrWhiteSpace(font.DisplayName)));
        Assert.All(fonts.Take(10), font => Assert.NotEmpty(font.FontFamily.GetTypefaces()));

        string[] names = fonts.Select(font => font.DisplayName).ToArray();
        string[] sortedNames = names
            .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        Assert.Equal(sortedNames, names);
    }
}
