# NetTopologySuite.GeoPackage.FeatureReader
Extension for NetTopologySuite to read a geoPackage into a list fo Features.
Uses [Microsoft.Data.Sqlite](https://learn.microsoft.com/de-de/dotnet/standard/data/sqlite/?tabs=netcore-cli) and [Dapper](https://github.com/DapperLib/Dapper) to read SqLite data, and [NetTopologySuite.IO.GeoPackage](https://github.com/NetTopologySuite/NetTopologySuite.IO.SpatiaLite) to transform the actual GeoData.
## Usage:
The most simple usage is to use the static read method, that returs all GeoPackage features layers
```csharp
var layers = GeoPackage.ReadGeoPackage("example.gpkg");
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

## License
MIT License
