using System.Reflection;

namespace WinPTP.Tests;

public sealed class ApplicationMetadataTests
{
    [Fact]
    public void CurrentAssemblyVersionCanBeRetrieved()
    {
        ApplicationMetadata metadata = ApplicationMetadata.FromAssembly(typeof(App).Assembly);

        Assert.Equal("0.1.0", metadata.Version);
    }

    [Fact]
    public void CurrentAssemblyBuildDateIsPresentAndParseable()
    {
        Assembly assembly = typeof(App).Assembly;
        AssemblyMetadataAttribute buildDateAttribute = Assert.Single(
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>(),
            attribute => attribute.Key == "BuildDate");

        Assert.True(DateOnly.TryParseExact(
            buildDateAttribute.Value,
            "yyyy-MM-dd",
            out DateOnly parsedDate));
        Assert.Equal(parsedDate, ApplicationMetadata.FromAssembly(assembly).BuildDate);
    }
}
