using CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader;
using CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter;
using FluentAssertions;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Tests;

public class BasicTests
{
    [Test]
    public void TestStaticMethod()
    {
        var content = GeoPackageFeatureReader.ReadGeoPackage("example.gpkg");
        content.Should().HaveCount(8);
        var testLayer = content.FirstOrDefault(layer => layer.Info.TableName == "point1");
        testLayer.Should().NotBeNull();
        testLayer!.Info.SrsId.Should().Be(4326);
        testLayer.Features.Should().HaveCount(4);
        var feature = testLayer.Features[0];
        var names = feature.Attributes.GetNames();
        names.Should().NotContain("geometry");
        names.Should().Contain("id");
        names.Should().Contain("date");
        var inUse = IsFileInUse("example.gpkg");
        inUse.Should().BeFalse("file should not be used by an other process");
    }

    [Test]
    public async Task RoundtripTest()
    {
        using var writer = new GeoPackageFeatureWriter("test.gpkg");
        await writer.AddSrsAsync(25832, "ETRS89 / UTM zone 32N", "PROJCS[\"ETRS89 / UTM zone 32N\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"25832\"]]", "EPSG", 25832);
        var polygons = new List<Feature>();
        for (var i = 0; i < 10; i++)
        {
            var x = i/10.0 + 10.0;
            var polygon = new Feature(new Polygon(new LinearRing(new[]
            {
                new Coordinate(x, 50.0),
                new Coordinate(x+0.1, 50.0),
                new Coordinate(x+0.1, 50.5),
                new Coordinate(x, 50.5),
                new Coordinate(x, 50.0)
            })), new AttributesTable
            {
                {"id", i},
                {"date", DateTime.Now}
            });
            polygons.Add(polygon);
        }
        
        var lines = new List<Feature>();
        for (var i = 0; i < 10; i++)
        {
            var x = i/10.0 + 10.0;
            var line = new Feature(new LineString(new[]
            {
                
                new Coordinate(x+0.01, 50.0),
                new Coordinate(x+0.03, 50.1),
                new Coordinate(x+0.05, 50.2),
                new Coordinate(x+0.07, 50.3),
                new Coordinate(x+0.09, 50.4)
            }), new AttributesTable
            {
                {"ID", i},
                {"date", DateTime.Now},
                {"internalID", $"X{i}"},
                {"testDouble", x /13.1}
            });
            lines.Add(line);
        }

        await writer.AddLayerAsync(polygons, "polygons");
        await writer.AddLayerAsync(lines, "lines");
        await writer.CloseAsync();

        var inUse = IsFileInUse("test.gpkg");
        inUse.Should().BeFalse("file should not be used by an other process after writer is closed");

        var reader = GeoPackageFeatureReader.ReadGeoPackage("test.gpkg");
        reader.Count.Should().Be(2);
        var polygonLayer  = reader.FirstOrDefault(l => l.Info.TableName == "polygons");
        polygonLayer.Should().NotBeNull();
        polygonLayer.Features.Should().HaveCount(10);
        var lineLayer  = reader.FirstOrDefault(l => l.Info.TableName == "lines");
        lineLayer.Should().NotBeNull();
        lineLayer.Features.Should().HaveCount(10);
        var testFeature = lineLayer.Features[0];
        testFeature.Attributes.Count.Should().Be(4);
        testFeature.Attributes.GetNames().Should().Contain(new[] {"ID", "date", "internalID", "testDouble"});
        testFeature.Attributes["ID"].Should().Be(0);
        testFeature.Attributes["internalID"].Should().Be("X0");
        testFeature.Attributes["testDouble"].Should().BeOfType(typeof(double));
        testFeature.Attributes["date"].Should().BeOfType(typeof(string));
    }

    private static bool IsFileInUse(string filename)
    {
        try
        {
            using FileStream fs = File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException) {
            return true;
        }
    }
}

