# GeoPackage Feature Reader/Writer

[![License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/claasd//NetTopologySuite.IO.GeoPackage.Feature/blob/main/LICENSE)
[![CI](https://github.com/claasd//NetTopologySuite.IO.GeoPackage.Feature/actions/workflows/build.yml/badge.svg)](https://github.com/claasd//NetTopologySuite.IO.GeoPackage/actions/workflows/build.yml)

Library to read and write GeoPackage files with NetTopologySuite.

# CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader

[![Nuget](https://img.shields.io/nuget/v/CdIts.Caffoa.Json.NetCdIts.NetTopologySuite.IO.GeoPackage.FeatureReader)](https://www.nuget.org/packages/CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader/)
[![Nuget](https://img.shields.io/nuget/vpre/CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader)](https://www.nuget.org/packages/CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader/)


Extension for NetTopologySuite to read a geoPackage into a list of Features.
Uses [Microsoft.Data.Sqlite](https://learn.microsoft.com/de-de/dotnet/standard/data/sqlite/?tabs=netcore-cli) and [Dapper](https://github.com/DapperLib/Dapper) to read SqLite data, and [NetTopologySuite.IO.GeoPackage](https://github.com/NetTopologySuite/NetTopologySuite.IO.SpatiaLite) to transform the actual GeoData.
## Usage:
The most simple usage is to use the static read method, that returs all GeoPackage features layers
```csharp
var layers = GeoPackageFeatureReader.ReadGeoPackage("example.gpkg");
foreach (var layer in layers)
{
    var name = layer.Info.Identifier;
    var features = layer.Features;
    var spatialReference = layer.GeoPackageSpatialReference;
}
```

you can also just load the features if you know the table name:

```csharp
var package = new GeoPackage(path);
var features = package.ReadFeature("tableName");
```

# CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter

[![Nuget](https://img.shields.io/nuget/v/CdIts.Caffoa.Json.NetCdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter)](https://www.nuget.org/packages/CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader/)
[![Nuget](https://img.shields.io/nuget/vpre/CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter)](https://www.nuget.org/packages/CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader/)

Extensions for NetTopologySuite to write a list of Features into a GeoPackage.
Uses [Microsoft.Data.Sqlite](https://learn.microsoft.com/de-de/dotnet/standard/data/sqlite/?tabs=netcore-cli) and [Dapper](https://github.com/DapperLib/Dapper) to write SqLite data, and [NetTopologySuite.IO.GeoPackage](https://github.com/NetTopologySuite/NetTopologySuite.IO.SpatiaLite) to transform the actual GeoData.

## Usage:
```csharp
Feature[] linesFeatures; // your features
Feature[] polygonFeatures; // your features
using (var writer = new GeoPackageFeatureWriter("test.gpkg")) {
    // by default , only EPSG:4326 is available, but you can add additional SRS
    writer.AddSrs(25832, "ETRS89 / UTM zone 32N", "PROJCS[\"ETRS89 / UTM zone 32N\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"25832\"]]", "EPSG", 25832);
    writer.AddLayer("lines", lineFeatures, 25832, "geom");
    // by default, the srs is 4326, and the name of the geometry column is 'geometry'
    writer.AddLayer("polygons", polygonFeatures);
}
```
everything is also supported as async methods:
```csharp
await using var writer = new GeoPackageFeatureWriter("test.gpkg");
await writer.AddSrsAsync(25832, "ETRS89 / UTM zone 32N", "PROJCS[\"ETRS89 / UTM zone 32N\",GEOGCS[\"ETRS89\",DATUM[\"European_Terrestrial_Reference_System_1989\",SPHEROID[\"GRS 1980\",6378137,298.257222101,AUTHORITY[\"EPSG\",\"7019\"]],TOWGS84[0,0,0,0,0,0,0],AUTHORITY[\"EPSG\",\"6258\"]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4258\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"25832\"]]", "EPSG", 25832);
await writer.AddLayerAsync("lines", lineFeatures, 25832, "geom");
await writer.AddLayerAsync("polygons", polygonFeatures);
// you can alos close the stream manually if you need to
await writer.CloseAsync();
```

## License
MIT License
