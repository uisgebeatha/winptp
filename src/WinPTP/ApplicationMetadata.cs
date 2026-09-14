using System.Globalization;
using System.Reflection;

namespace WinPTP;

internal sealed record ApplicationMetadata(string Version, DateOnly BuildDate)
{
    private const string BuildDateMetadataKey = "BuildDate";

    public static ApplicationMetadata Current { get; } = FromAssembly(typeof(App).Assembly);

    public string BuildDateText => BuildDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    internal static ApplicationMetadata FromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        string version = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion
            .Split('+', 2)[0]
            ?? assembly.GetName().Version?.ToString(3)
            ?? throw new InvalidOperationException("Application version metadata is missing.");

        string? buildDateValue = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == BuildDateMetadataKey)?
            .Value;

        if (!DateOnly.TryParseExact(
                buildDateValue,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly buildDate))
        {
            throw new InvalidOperationException("Application build-date metadata is missing or invalid.");
        }

        return new ApplicationMetadata(version, buildDate);
    }
}
