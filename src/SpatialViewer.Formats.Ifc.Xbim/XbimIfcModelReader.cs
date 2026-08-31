using System.Collections;
using System.Globalization;
using System.Reflection;
using SpatialViewer.Core;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;

namespace SpatialViewer.Formats.Ifc.Xbim;

public sealed class XbimIfcModelReader : IIfcModelReader
{
    public ValueTask<IfcLoadResult> OpenAsync(
        string path,
        IfcOpenOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        options ??= new IfcOpenOptions();

        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The IFC model was not found.", path);
        }

        return new ValueTask<IfcLoadResult>(Task.Run(
            () => OpenCore(path, options, cancellationToken),
            cancellationToken));
    }

    private static IfcLoadResult OpenCore(
        string path,
        IfcOpenOptions options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Report(options, IfcLoadStage.Opening, 0, "Opening IFC model");

        ReportProgressDelegate progress = (percentProgress, userState) =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = userState?.ToString() ?? "Parsing IFC entities";
            Report(options, IfcLoadStage.Parsing, Math.Clamp(percentProgress, 0, 100), message);
        };

        using var model = IfcStore.Open(path, null, -1d, progress);
        cancellationToken.ThrowIfCancellationRequested();

        var schema = MapSchema(model.SchemaVersion);
        var diagnostics = new List<IfcLoadDiagnostic>();
        if (schema == IfcSchemaVersion.Unknown)
        {
            diagnostics.Add(new IfcLoadDiagnostic(
                "IFC_SCHEMA_UNKNOWN",
                $"The xBIM schema '{model.SchemaVersion}' is not mapped by SpatialViewer.IFCCore.",
                true));
        }

        if (options.IncludeGeometry)
        {
            diagnostics.Add(new IfcLoadDiagnostic(
                "IFC_GEOMETRY_DEFERRED",
                "Geometry extraction is scheduled for the 0.3.x geometry pipeline; 0.2.x loads semantic BIM data only."));
        }

        Report(options, IfcLoadStage.BuildingHierarchy, 0, "Building IFC spatial hierarchy");
        var document = BuildDocument(model, path, schema, options, diagnostics, cancellationToken);
        Report(options, IfcLoadStage.Completed, 100, "IFC model loaded");
        return new IfcLoadResult(document, schema, diagnostics);
    }

    private static SceneDocument BuildDocument(
        IfcStore model,
        string path,
        IfcSchemaVersion schema,
        IfcOpenOptions options,
        List<IfcLoadDiagnostic> diagnostics,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<int>();
        var projects = model.Instances.OfType<IIfcProject>().ToList();
        var children = new List<SceneNode>(projects.Count + 1);

        if (projects.Count == 0)
        {
            diagnostics.Add(new IfcLoadDiagnostic(
                "IFC_PROJECT_MISSING",
                "No IfcProject was found. Uncontained occurrences are still exposed when possible."));
        }

        for (var index = 0; index < projects.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            children.Add(BuildNode(projects[index], null, options, visited, cancellationToken));
            Report(
                options,
                IfcLoadStage.BuildingHierarchy,
                GetPercent(index + 1, Math.Max(projects.Count, 1)),
                $"Building project hierarchy {index + 1}/{projects.Count}");
        }

        var uncontained = model.Instances
            .OfType<IIfcObject>()
            .Where(item => !visited.Contains(item.EntityLabel))
            .ToList();

        if (uncontained.Count > 0)
        {
            var orphanNodes = new List<SceneNode>(uncontained.Count);
            foreach (var item in uncontained)
            {
                cancellationToken.ThrowIfCancellationRequested();
                orphanNodes.Add(BuildNode(item, null, options, visited, cancellationToken));
            }

            children.Add(new SceneNode(
                "ifc:uncontained",
                "Uncontained",
                "IFC.Uncontained",
                null,
                [new SceneProperty("Count", uncontained.Count.ToString(CultureInfo.InvariantCulture), null, "IFC")],
                [],
                orphanNodes));

            diagnostics.Add(new IfcLoadDiagnostic(
                "IFC_UNCONTAINED_OBJECTS",
                $"{uncontained.Count.ToString(CultureInfo.InvariantCulture)} IFC object occurrences were not reached through the primary spatial hierarchy."));
        }

        var fileName = Path.GetFileName(path);
        var root = new SceneNode(
            "ifc:root",
            fileName,
            "IFC",
            null,
            [],
            [],
            children);

        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Format"] = "IFC",
            ["Schema"] = schema.ToString(),
            ["SourcePath"] = Path.GetFullPath(path),
            ["EntityCount"] = model.Instances.Count.ToString(CultureInfo.InvariantCulture),
        };

        return new SceneDocument(fileName, root, SceneBounds.Empty, metadata);
    }

    private static SceneNode BuildNode(
        IIfcObjectDefinition definition,
        IIfcObjectDefinition? parent,
        IfcOpenOptions options,
        HashSet<int> visited,
        CancellationToken cancellationToken)
    {
        if (!visited.Add(definition.EntityLabel))
        {
            return CreateReferenceNode(definition);
        }

        cancellationToken.ThrowIfCancellationRequested();
        var properties = ExtractProperties(definition, parent, options.IncludeProperties);
        var childDefinitions = GetChildDefinitions(definition)
            .Where(child => child.EntityLabel != definition.EntityLabel)
            .GroupBy(child => child.EntityLabel)
            .Select(group => group.First())
            .ToList();

        var children = new List<SceneNode>(childDefinitions.Count);
        foreach (var child in childDefinitions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            children.Add(visited.Contains(child.EntityLabel)
                ? CreateReferenceNode(child)
                : BuildNode(child, definition, options, visited, cancellationToken));
        }

        var category = definition.ExpressType.Name;
        var name = GetText(definition.Name) ?? $"{category} #{definition.EntityLabel.ToString(CultureInfo.InvariantCulture)}";
        return new SceneNode(
            $"ifc:{definition.EntityLabel.ToString(CultureInfo.InvariantCulture)}",
            name,
            category,
            definition.GlobalId.ToString(),
            properties,
            [],
            children);
    }

    private static SceneNode CreateReferenceNode(IIfcObjectDefinition definition)
    {
        var category = definition.ExpressType.Name;
        var name = GetText(definition.Name) ?? $"{category} #{definition.EntityLabel.ToString(CultureInfo.InvariantCulture)}";
        return new SceneNode(
            $"ifc-ref:{definition.EntityLabel.ToString(CultureInfo.InvariantCulture)}",
            name,
            $"{category}.Reference",
            definition.GlobalId.ToString(),
            [new SceneProperty("ReferenceTo", $"ifc:{definition.EntityLabel.ToString(CultureInfo.InvariantCulture)}", null, "IFC")],
            [],
            []);
    }

    private static IEnumerable<IIfcObjectDefinition> GetChildDefinitions(IIfcObjectDefinition definition)
    {
        foreach (var relation in definition.IsDecomposedBy)
        {
            foreach (var related in relation.RelatedObjects)
            {
                yield return related;
            }
        }

        foreach (var relation in definition.IsNestedBy)
        {
            foreach (var related in relation.RelatedObjects)
            {
                yield return related;
            }
        }

        if (definition is IIfcSpatialElement spatial)
        {
            foreach (var relation in spatial.ContainsElements)
            {
                foreach (var element in relation.RelatedElements)
                {
                    yield return element;
                }
            }
        }
    }

    private static IReadOnlyList<SceneProperty> ExtractProperties(
        IIfcObjectDefinition definition,
        IIfcObjectDefinition? parent,
        bool includeProperties)
    {
        var result = new List<SceneProperty>
        {
            new("GlobalId", definition.GlobalId.ToString(), null, "IFC"),
            new("Class", definition.ExpressType.Name, null, "IFC"),
            new("EntityLabel", definition.EntityLabel.ToString(CultureInfo.InvariantCulture), null, "IFC"),
        };

        AddIfPresent(result, "Description", GetText(definition.Description), "IFC");
        if (parent is not null)
        {
            AddIfPresent(result, "SpatialContainer", GetText(parent.Name) ?? parent.ExpressType.Name, "IFC");
            AddIfPresent(result, "SpatialContainerGlobalId", parent.GlobalId.ToString(), "IFC");
        }

        if (definition is IIfcObject occurrence)
        {
            AddIfPresent(result, "ObjectType", GetText(occurrence.ObjectType), "IFC");
            foreach (var relation in occurrence.IsTypedBy)
            {
                var type = relation.RelatingType;
                AddIfPresent(result, "TypeName", GetText(type.Name), "IFC.Type");
                AddIfPresent(result, "TypeClass", type.ExpressType.Name, "IFC.Type");
                AddIfPresent(result, "TypeGlobalId", type.GlobalId.ToString(), "IFC.Type");

                if (includeProperties)
                {
                    foreach (var propertySet in type.HasPropertySets)
                    {
                        AppendPropertySet(result, propertySet, $"Type.{GetText(propertySet.Name) ?? propertySet.ExpressType.Name}");
                    }
                }
            }

            if (includeProperties)
            {
                foreach (var relation in occurrence.IsDefinedBy)
                {
                    AppendPropertyDefinition(result, relation.RelatingPropertyDefinition);
                }
            }
        }

        foreach (var association in definition.HasAssociations.OfType<IIfcRelAssociatesClassification>())
        {
            AddIfPresent(result, "Classification", DescribeSelect(association.RelatingClassification), "IFC.Classification");
        }

        AddIfPresent(result, "Material", DescribeSelect(definition.Material), "IFC.Material");
        return result;
    }

    private static void AppendPropertyDefinition(List<SceneProperty> result, IIfcPropertySetDefinitionSelect definition)
    {
        foreach (var propertySet in definition.PropertySetDefinitions)
        {
            AppendPropertySet(result, propertySet, GetText(propertySet.Name) ?? propertySet.ExpressType.Name);
        }
    }

    private static void AppendPropertySet(List<SceneProperty> result, IIfcPropertySetDefinition definition, string group)
    {
        if (definition is IIfcPropertySet propertySet)
        {
            foreach (var property in propertySet.HasProperties)
            {
                AddIfPresent(result, GetText(property.Name) ?? property.ExpressType.Name, GetPropertyValue(property), group, GetUnit(property));
            }

            return;
        }

        if (definition is IIfcElementQuantity quantities)
        {
            foreach (var quantity in quantities.Quantities)
            {
                AddIfPresent(result, GetText(quantity.Name) ?? quantity.ExpressType.Name, GetQuantityValue(quantity), group, GetUnit(quantity));
            }
        }
    }

    private static string? GetPropertyValue(IIfcProperty property)
    {
        if (property is IIfcPropertySingleValue singleValue)
        {
            return FormatValue(singleValue.NominalValue);
        }

        return GetFirstNamedValue(
            property,
            "EnumerationValues",
            "ListValues",
            "UpperBoundValue",
            "LowerBoundValue",
            "SetPointValue",
            "PropertyReference",
            "DefinedValues");
    }

    private static string? GetQuantityValue(IIfcPhysicalQuantity quantity)
    {
        var valueProperty = quantity.GetType()
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.CanRead && property.Name.EndsWith("Value", StringComparison.Ordinal))
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .FirstOrDefault();

        return valueProperty is null ? null : FormatValue(valueProperty.GetValue(quantity));
    }

    private static string? GetUnit(object source)
    {
        var property = source.GetType().GetProperty("Unit", BindingFlags.Instance | BindingFlags.Public);
        return property is null ? null : DescribeSelect(property.GetValue(source));
    }

    private static string? GetFirstNamedValue(object source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            var property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property is null || !property.CanRead)
            {
                continue;
            }

            var value = FormatValue(property.GetValue(source));
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? DescribeSelect(object? value)
    {
        if (value is null)
        {
            return null;
        }

        foreach (var propertyName in new[] { "Name", "Identification", "ItemReference", "Location" })
        {
            var property = value.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property is null || !property.CanRead)
            {
                continue;
            }

            var text = FormatValue(property.GetValue(value));
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return FormatValue(value);
    }

    private static string? FormatValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        if (value is IEnumerable enumerable)
        {
            var values = new List<string>();
            foreach (var item in enumerable)
            {
                var formatted = FormatValue(item);
                if (!string.IsNullOrWhiteSpace(formatted))
                {
                    values.Add(formatted);
                }
            }

            return values.Count == 0 ? null : string.Join(", ", values);
        }

        return value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value.ToString();
    }

    private static string? GetText(object? value) => FormatValue(value);

    private static void AddIfPresent(
        List<SceneProperty> result,
        string name,
        string? value,
        string group,
        string? unit = null)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            result.Add(new SceneProperty(name, value, unit, group));
        }
    }

    private static IfcSchemaVersion MapSchema(XbimSchemaVersion schema) => schema switch
    {
        XbimSchemaVersion.Ifc2X3 => IfcSchemaVersion.Ifc2X3,
        XbimSchemaVersion.Ifc4 => IfcSchemaVersion.Ifc4,
        XbimSchemaVersion.Ifc4x3 => IfcSchemaVersion.Ifc4X3,
        _ => IfcSchemaVersion.Unknown,
    };

    private static int GetPercent(int completed, int total) =>
        total <= 0 ? 100 : Math.Clamp((int)Math.Round(completed * 100d / total), 0, 100);

    private static void Report(IfcOpenOptions options, IfcLoadStage stage, int percent, string message) =>
        options.Progress?.Report(new IfcLoadProgress(stage, percent, message));
}
