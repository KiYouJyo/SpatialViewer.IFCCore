using System.Globalization;
using System.IO.Compression;

namespace SpatialViewer.Formats.Ifc.Tests;

internal static class IfcTestFile
{
    public static string WriteHeaderOnly(string schema) => Write(CreateHeaderOnly(schema));

    public static string WriteHeaderOnlyIfcZip(string schema)
    {
        var ifcPath = WriteHeaderOnly(schema);
        var zipPath = Path.ChangeExtension(ifcPath, ".ifczip");
        try
        {
            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(ifcPath, "model.ifc", CompressionLevel.Fastest);
        }
        finally
        {
            File.Delete(ifcPath);
        }

        return zipPath;
    }

    public static string WriteSemanticIfc4()
    {
        const string content = """
            ISO-10303-21;
            HEADER;
            FILE_DESCRIPTION(('SpatialViewer.IFCCore semantic fixture'),'2;1');
            FILE_NAME('semantic.ifc','2026-08-31T00:00:00',('SpatialViewer'),('SpatialViewer'),'SpatialViewer.IFCCore','SpatialViewer.IFCCore','');
            FILE_SCHEMA(('IFC4'));
            ENDSEC;
            DATA;
            #1=IFCPERSON($,'Tester','SpatialViewer',$,$,$,$,$);
            #2=IFCORGANIZATION($,'SpatialViewer',$,$,$);
            #3=IFCPERSONANDORGANIZATION(#1,#2,$);
            #4=IFCAPPLICATION(#2,'0.3.0','SpatialViewer.IFCCore','SVIFC');
            #5=IFCOWNERHISTORY(#3,#4,$,.ADDED.,$,$,$,0);
            #6=IFCSIUNIT(*,.LENGTHUNIT.,$,.METRE.);
            #7=IFCSIUNIT(*,.AREAUNIT.,$,.SQUARE_METRE.);
            #8=IFCSIUNIT(*,.VOLUMEUNIT.,$,.CUBIC_METRE.);
            #9=IFCUNITASSIGNMENT((#6,#7,#8));
            #10=IFCCARTESIANPOINT((0.,0.,0.));
            #11=IFCAXIS2PLACEMENT3D(#10,$,$);
            #12=IFCGEOMETRICREPRESENTATIONCONTEXT($,'Model',3,1.E-05,#11,$);
            #20=IFCPROJECT('0JQYwJX0X1A9Z8v7n6m5k4',#5,'Project',$,$,$,$,(#12),#9);
            #21=IFCSITE('1JQYwJX0X1A9Z8v7n6m5k4',#5,'Site',$,$,$,$,$,.ELEMENT.,$,$,$,$,$);
            #22=IFCBUILDING('2JQYwJX0X1A9Z8v7n6m5k4',#5,'Building',$,$,$,$,$,.ELEMENT.,$,$,$);
            #23=IFCBUILDINGSTOREY('3JQYwJX0X1A9Z8v7n6m5k4',#5,'Storey',$,$,$,$,$,.ELEMENT.,0.);
            #24=IFCWALL('3hW0Q0YqP0k8oT7M2h4abc',#5,'Wall 01',$,$,$,$,'W-01',.NOTDEFINED.);
            #30=IFCRELAGGREGATES('0aQYwJX0X1A9Z8v7n6m5k4',#5,$,$,#20,(#21));
            #31=IFCRELAGGREGATES('0bQYwJX0X1A9Z8v7n6m5k4',#5,$,$,#21,(#22));
            #32=IFCRELAGGREGATES('0cQYwJX0X1A9Z8v7n6m5k4',#5,$,$,#22,(#23));
            #33=IFCRELCONTAINEDINSPATIALSTRUCTURE('0dQYwJX0X1A9Z8v7n6m5k4',#5,$,$,(#24),#23);
            #40=IFCPROPERTYSINGLEVALUE('Reference',$,IFCLABEL('W-01'),$);
            #41=IFCPROPERTYSINGLEVALUE('IsExternal',$,IFCBOOLEAN(.T.),$);
            #42=IFCPROPERTYSET('0eQYwJX0X1A9Z8v7n6m5k4',#5,'Pset_WallCommon',$,(#40,#41));
            #43=IFCRELDEFINESBYPROPERTIES('0fQYwJX0X1A9Z8v7n6m5k4',#5,$,$,(#24),#42);
            #44=IFCQUANTITYLENGTH('Length',$,$,5.,$);
            #45=IFCELEMENTQUANTITY('0gQYwJX0X1A9Z8v7n6m5k4',#5,'BaseQuantities',$,$,(#44));
            #46=IFCRELDEFINESBYPROPERTIES('0hQYwJX0X1A9Z8v7n6m5k4',#5,$,$,(#24),#45);
            #50=IFCMATERIAL('Concrete',$,$);
            #51=IFCRELASSOCIATESMATERIAL('0iQYwJX0X1A9Z8v7n6m5k4',#5,$,$,(#24),#50);
            #52=IFCCLASSIFICATION($,$,$,'Uniclass',$,$,$);
            #53=IFCCLASSIFICATIONREFERENCE($,'EF_25','Walls',#52,$,$);
            #54=IFCRELASSOCIATESCLASSIFICATION('0jQYwJX0X1A9Z8v7n6m5k4',#5,$,$,(#24),#53);
            ENDSEC;
            END-ISO-10303-21;
            """;
        return Write(content);
    }

    public static string WriteGeometryIfc4(double xMillimetres = 10_000d)
    {
        var x = xMillimetres.ToString("R", CultureInfo.InvariantCulture);
        var xSecond = (xMillimetres + 5_000d).ToString("R", CultureInfo.InvariantCulture);
        var content = $$"""
            ISO-10303-21;
            HEADER;
            FILE_DESCRIPTION(('SpatialViewer.IFCCore geometry fixture'),'2;1');
            FILE_NAME('geometry.ifc','2026-08-31T00:00:00',('SpatialViewer'),('SpatialViewer'),'SpatialViewer.IFCCore','SpatialViewer.IFCCore','');
            FILE_SCHEMA(('IFC4'));
            ENDSEC;
            DATA;
            #1=IFCPERSON($,'Tester','SpatialViewer',$,$,$,$,$);
            #2=IFCORGANIZATION($,'SpatialViewer',$,$,$);
            #3=IFCPERSONANDORGANIZATION(#1,#2,$);
            #4=IFCAPPLICATION(#2,'0.3.0','SpatialViewer.IFCCore','SVIFC');
            #5=IFCOWNERHISTORY(#3,#4,$,.ADDED.,$,$,$,0);
            #6=IFCSIUNIT(*,.LENGTHUNIT.,.MILLI.,.METRE.);
            #7=IFCSIUNIT(*,.AREAUNIT.,.MILLI.,.SQUARE_METRE.);
            #8=IFCSIUNIT(*,.VOLUMEUNIT.,.MILLI.,.CUBIC_METRE.);
            #9=IFCUNITASSIGNMENT((#6,#7,#8));
            #10=IFCCARTESIANPOINT((0.,0.,0.));
            #11=IFCAXIS2PLACEMENT3D(#10,$,$);
            #12=IFCGEOMETRICREPRESENTATIONCONTEXT($,'Model',3,1.E-05,#11,$);
            #20=IFCPROJECT('0JQYwJX0X1A9Z8v7n6m5k4',#5,'Project',$,$,$,$,(#12),#9);
            #21=IFCSITE('1JQYwJX0X1A9Z8v7n6m5k4',#5,'Site',$,$,$,$,$,.ELEMENT.,$,$,$,$,$);
            #22=IFCBUILDING('2JQYwJX0X1A9Z8v7n6m5k4',#5,'Building',$,$,$,$,$,.ELEMENT.,$,$,$);
            #23=IFCBUILDINGSTOREY('3JQYwJX0X1A9Z8v7n6m5k4',#5,'Storey',$,$,$,$,$,.ELEMENT.,0.);
            #60=IFCCARTESIANPOINT(({{x}},20000.,30000.));
            #61=IFCAXIS2PLACEMENT3D(#60,$,$);
            #62=IFCLOCALPLACEMENT($,#61);
            #63=IFCCARTESIANPOINT((0.,0.));
            #64=IFCAXIS2PLACEMENT2D(#63,$);
            #65=IFCRECTANGLEPROFILEDEF(.AREA.,'WallProfile',#64,2000.,1000.);
            #66=IFCCARTESIANPOINT((0.,0.,0.));
            #67=IFCAXIS2PLACEMENT3D(#66,$,$);
            #68=IFCDIRECTION((0.,0.,1.));
            #69=IFCEXTRUDEDAREASOLID(#65,#67,#68,3000.);
            #70=IFCSHAPEREPRESENTATION(#12,'Body','SweptSolid',(#69));
            #71=IFCPRODUCTDEFINITIONSHAPE($,$,(#70));
            #80=IFCCARTESIANPOINT(({{xSecond}},20000.,30000.));
            #81=IFCAXIS2PLACEMENT3D(#80,$,$);
            #82=IFCLOCALPLACEMENT($,#81);
            #24=IFCWALL('3hW0Q0YqP0k8oT7M2h4abc',#5,'Wall 01',$,$,#62,#71,'W-01',.NOTDEFINED.);
            #25=IFCWALL('2hW0Q0YqP0k8oT7M2h4abd',#5,'Wall 02',$,$,#82,#71,'W-02',.NOTDEFINED.);
            #30=IFCRELAGGREGATES('0aQYwJX0X1A9Z8v7n6m5k4',#5,$,$,#20,(#21));
            #31=IFCRELAGGREGATES('0bQYwJX0X1A9Z8v7n6m5k4',#5,$,$,#21,(#22));
            #32=IFCRELAGGREGATES('0cQYwJX0X1A9Z8v7n6m5k4',#5,$,$,#22,(#23));
            #33=IFCRELCONTAINEDINSPATIALSTRUCTURE('0dQYwJX0X1A9Z8v7n6m5k4',#5,$,$,(#24,#25),#23);
            ENDSEC;
            END-ISO-10303-21;
            """;
        return Write(content);
    }

    private static string CreateHeaderOnly(string schema) => $$"""
        ISO-10303-21;
        HEADER;
        FILE_DESCRIPTION(('SpatialViewer.IFCCore schema fixture'),'2;1');
        FILE_NAME('schema.ifc','2026-08-31T00:00:00',('SpatialViewer'),('SpatialViewer'),'SpatialViewer.IFCCore','SpatialViewer.IFCCore','');
        FILE_SCHEMA(('{{schema}}'));
        ENDSEC;
        DATA;
        ENDSEC;
        END-ISO-10303-21;
        """;

    private static string Write(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"SpatialViewer.IFCCore-{Guid.NewGuid():N}.ifc");
        File.WriteAllText(path, content);
        return path;
    }
}
