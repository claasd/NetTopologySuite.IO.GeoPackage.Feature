// See https://aka.ms/new-console-template for more information

using CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader;
using CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter;
using Microsoft.Extensions.Logging;

using var factory = LoggerFactory.Create(builder => builder.AddConsole());
var logger = factory.CreateLogger("GeoPackage");


var reader = GeoPackageFeatureReader.ReadGeoPackage(@"C:\Users\ClaasDiederichs\Downloads\CEL_HUSTEDT.gpkg", false, logger);
Console.WriteLine(reader.Count);

await using var writer = new GeoPackageFeatureWriter("test.gpkg");
var srs = reader.First().GeoPackageSpatialReference!;
writer.AddSrs(srs.SrsId, srs.SrsName, srs.Definition, srs.Organization, srs.OrganizationCoordsysId, srs.Description);
foreach (var layer in reader)
{
    if(layer.Features.Any())
        await writer.AddLayerAsync(layer.Features, layer.Info.TableName, layer.Info.SrsId);    
}
