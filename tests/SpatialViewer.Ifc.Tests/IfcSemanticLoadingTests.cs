using SpatialViewer.Core;
using SpatialViewer.Formats.Ifc.Xbim;
using Xunit;

namespace SpatialViewer.Formats.Ifc.Tests;

public sealed class IfcSemanticLoadingTests
{
    [Fact]
    public async Task ReaderBuildsSpatialTreeAndExtractsCommonMetadata()
    {
        var path = IfcTestFile.WriteSemanticIfc4();
        try
        {
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(path);

            Assert.Equal(IfcSchemaVersion.Ifc4, result.Schema);
            var project = Assert.Single(result.Document.Root.Children.Where(node => node.Category == "IfcProject"));
            var site = Assert.Single(project.Children.Where(node => node.Category == "IfcSite"));
            var building = Assert.Single(site.Children.Where(node => node.Category == "IfcBuilding"));
            var storey = Assert.Single(building.Children.Where(node => node.Category == "IfcBuildingStorey"));
            var wall = Assert.Single(storey.Children.Where(node => node.Category == "IfcWall"));

            Assert.Equal("Wall 01", wall.Name);
            Assert.Contains(wall.Properties.Values, property => IsProperty(property, "GlobalId", "3hW0Q0YqP0k8oT7M2h4abc"));
            Assert.Contains(wall.Properties.Values, property => IsProperty(property, "Reference", "W-01", "Pset_WallCommon"));
            Assert.Contains(wall.Properties.Values, property => IsProperty(property, "Length", "5", "BaseQuantities"));
            Assert.Contains(
                wall.Properties.Values,
                property => property.Group == "IFC.Material" && property.Value?.Contains("Concrete", StringComparison.Ordinal) == true);
            Assert.Contains(
                wall.Properties.Values,
                property => property.Group == "IFC.Classification" && property.Value?.Contains("Walls", StringComparison.Ordinal) == true);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static bool IsProperty(SceneProperty property, string name, string value, string? group = null) =>
        property.Name == name &&
        property.Value == value &&
        (group is null || property.Group == group);
}
