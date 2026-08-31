using SpatialViewer.Formats.Ifc.Xbim;
using Xunit;

namespace SpatialViewer.Formats.Ifc.Tests;

public sealed class IfcContractTests
{
    [Theory]
    [InlineData("IFC2X3", IfcSchemaVersion.Ifc2X3)]
    [InlineData("IFC4", IfcSchemaVersion.Ifc4)]
    [InlineData("IFC4X3_ADD2", IfcSchemaVersion.Ifc4X3)]
    public async Task ReaderDetectsSupportedSchemas(string schemaHeader, IfcSchemaVersion expected)
    {
        var path = IfcTestFile.WriteHeaderOnly(schemaHeader);
        try
        {
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(path);

            Assert.Equal(expected, result.SchemaVersion);
            Assert.Equal(expected.ToString(), result.Document.Metadata["Schema"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReaderHonorsPreCancelledToken()
    {
        var path = IfcTestFile.WriteHeaderOnly("IFC4");
        try
        {
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();
            var reader = new XbimIfcModelReader();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await reader.OpenAsync(path, cancellationToken: cancellation.Token);
            });
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReaderReportsGeometryDeferralWithoutFailingSemanticLoad()
    {
        var path = IfcTestFile.WriteHeaderOnly("IFC4");
        try
        {
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(path, new IfcOpenOptions { IncludeGeometry = true });

            Assert.Contains(result.Diagnostics, item => item.Code == "IFC_GEOMETRY_DEFERRED" && !item.IsError);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
