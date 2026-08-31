using SpatialViewer.Core;
using SpatialViewer.Formats.Ifc.Xbim;
using Xunit;

namespace SpatialViewer.Formats.Ifc.Tests;

public sealed class GeometryPipelineTests
{
    [Fact]
    public async Task ReaderGeneratesTriangulatedGeometryAndNormalizesMillimetresToMetres()
    {
        var path = IfcTestFile.WriteGeometryIfc4();
        try
        {
            var progress = new RecordingProgress();
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(
                path,
                new IfcOpenOptions { IncludeGeometry = true, Progress = progress });

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == IfcDiagnosticSeverity.Error);
            Assert.Equal("metre", result.Document.Metadata["Geometry.Unit"]);
            Assert.Equal("2", result.Document.Metadata["Geometry.InstanceCount"]);
            Assert.Equal("1", result.Document.Metadata["Geometry.UniqueMeshCount"]);

            var walls = FindNodes(result.Document.Root, "IfcWall").ToList();
            Assert.Equal(2, walls.Count);
            var firstGeometry = Assert.Single(walls[0].Children, node => node.Category == "IFC.Geometry");
            var secondGeometry = Assert.Single(walls[1].Children, node => node.Category == "IFC.Geometry");
            var firstMesh = Assert.Single(firstGeometry.Meshes);
            var secondMesh = Assert.Single(secondGeometry.Meshes);

            Assert.Same(firstMesh, secondMesh);
            Assert.True(firstMesh.TriangleCount >= 12);
            Assert.NotNull(firstMesh.Normals);
            Assert.Equal(firstMesh.Positions.Count, firstMesh.Normals!.Count);
            Assert.NotNull(firstMesh.MaterialId);
            Assert.StartsWith("xbim-style:", firstMesh.MaterialId, StringComparison.Ordinal);
            Assert.InRange(firstMesh.Bounds.Size.X, 1.99f, 2.01f);
            Assert.InRange(firstMesh.Bounds.Size.Y, 0.99f, 1.01f);
            Assert.InRange(firstMesh.Bounds.Size.Z, 2.99f, 3.01f);
            Assert.InRange(firstGeometry.Transform.M41, 9.99f, 10.01f);
            Assert.InRange(firstGeometry.Transform.M42, 19.99f, 20.01f);
            Assert.InRange(firstGeometry.Transform.M43, 29.99f, 30.01f);
            Assert.NotNull(result.Document.WorldBounds);
            Assert.InRange(result.Document.WorldBounds!.Value.Min.X, 8.99f, 9.01f);
            Assert.InRange(result.Document.WorldBounds.Value.Max.X, 15.99f, 16.01f);
            Assert.Contains(progress.Events, item => item.Stage == IfcLoadStage.GeneratingGeometry);
            Assert.Contains(progress.Events, item => item.Stage == IfcLoadStage.ExtractingGeometry);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReaderPreservesMappedMirrorWinding()
    {
        var path = IfcTestFile.WriteMappedMirroredIfc4();
        try
        {
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(path, new IfcOpenOptions { IncludeGeometry = true });

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == IfcDiagnosticSeverity.Error);
            var wall = Assert.Single(FindNodes(result.Document.Root, "IfcWall"));
            var geometry = Assert.Single(wall.Children, node => node.Category == "IFC.Geometry");
            Assert.True(geometry.FlipWinding);
            Assert.True(geometry.Transform.GetDeterminant() < 0f);
            Assert.NotNull(geometry.Bounds);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReaderCutsOpeningsWithoutRenderingFeatureElementsByDefault()
    {
        var path = IfcTestFile.WriteOpeningIfc4();
        try
        {
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(path, new IfcOpenOptions { IncludeGeometry = true });

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == IfcDiagnosticSeverity.Error);
            var wall = Assert.Single(FindNodes(result.Document.Root, "IfcWall"));
            var wallGeometry = Assert.Single(wall.Children, node => node.Category == "IFC.Geometry");
            var wallMesh = Assert.Single(wallGeometry.Meshes);
            Assert.True(wallMesh.TriangleCount > 12);

            var opening = Assert.Single(FindNodes(result.Document.Root, "IfcOpeningElement"));
            Assert.DoesNotContain(opening.Children, node => node.Category == "IFC.Geometry");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReaderCanPreserveOpeningElementGeometryOnRequest()
    {
        var path = IfcTestFile.WriteOpeningIfc4();
        try
        {
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(
                path,
                new IfcOpenOptions { IncludeGeometry = true, PreserveOpeningElements = true });

            Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == IfcDiagnosticSeverity.Error);
            var opening = Assert.Single(FindNodes(result.Document.Root, "IfcOpeningElement"));
            var openingGeometry = Assert.Single(opening.Children, node => node.Category == "IFC.Geometry");
            Assert.NotEmpty(openingGeometry.Meshes);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReaderRebasesLargeCoordinatesWhilePreservingWorldBounds()
    {
        var path = IfcTestFile.WriteGeometryIfc4(100_000_000d);
        try
        {
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(path, new IfcOpenOptions { IncludeGeometry = true });

            Assert.NotNull(result.Document.WorldBounds);
            Assert.NotNull(result.Document.Bounds);
            Assert.True(result.Document.WorldOrigin.X > 90_000f);
            Assert.True(Math.Abs(result.Document.Bounds!.Value.Center.X) < 0.01f);
            Assert.True(result.Document.WorldBounds!.Value.Center.X > 90_000f);
            Assert.NotEqual("0,0,0", result.Document.Metadata["Geometry.WorldOrigin"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task ReaderCanDisableLargeCoordinateRebasing()
    {
        var path = IfcTestFile.WriteGeometryIfc4(100_000_000d);
        try
        {
            var reader = new XbimIfcModelReader();
            var result = await reader.OpenAsync(
                path,
                new IfcOpenOptions { IncludeGeometry = true, RebaseLargeCoordinates = false });

            Assert.Equal(0f, result.Document.WorldOrigin.X);
            Assert.True(result.Document.Bounds!.Value.Center.X > 90_000f);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static IEnumerable<SceneNode> FindNodes(SceneNode node, string category)
    {
        if (node.Category == category)
        {
            yield return node;
        }

        foreach (var child in node.Children)
        {
            foreach (var match in FindNodes(child, category))
            {
                yield return match;
            }
        }
    }

    private sealed class RecordingProgress : IProgress<IfcLoadProgress>
    {
        public List<IfcLoadProgress> Events { get; } = [];

        public void Report(IfcLoadProgress value) => Events.Add(value);
    }
}
