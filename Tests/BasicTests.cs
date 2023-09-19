using CdIts.NetTopologySuite.GeoPackageFeatureReader;
using FluentAssertions;

namespace Tests;

public class BasicTests
{
    [Test]
    public void TestStaticMethod()
    {
        var content = GeoPackage.ReadGeoPackage("example.gpkg");
        content.Should().HaveCount(8);
        var testLayer = content.FirstOrDefault(layer=>layer.Info.TableName == "point1");
        testLayer.Should().NotBeNull();
        testLayer!.Info.SrsId.Should().Be(4326);
        testLayer.Features.Should().HaveCount(4);
        var feature = testLayer.Features[0];
        var names = feature.Attributes.GetNames();
        names.Should().NotContain("geometry");
        names.Should().Contain("id");
        names.Should().Contain("date");
    }
}