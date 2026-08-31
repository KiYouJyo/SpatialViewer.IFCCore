using SpatialViewer.Formats.Ifc;
using SpatialViewer.Formats.Ifc.Xbim;
using Xunit;

namespace SpatialViewer.Formats.Ifc.Tests;

public sealed class IfcContractTests
{
    [Fact]
    public async Task Foundation_adapter_fails_explicitly_until_xbim_is_integrated()
    {
        IIfcModelReader reader = new XbimIfcModelReader();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
        {
            await reader.OpenAsync("sample.ifc");
        });

        Assert.Contains("Phase 1", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(IfcSchemaVersion.Ifc2X3)]
    [InlineData(IfcSchemaVersion.Ifc4)]
    [InlineData(IfcSchemaVersion.Ifc4X3)]
    public void Target_schema_values_are_explicit(IfcSchemaVersion schema)
    {
        Assert.NotEqual(IfcSchemaVersion.Unknown, schema);
    }
}
