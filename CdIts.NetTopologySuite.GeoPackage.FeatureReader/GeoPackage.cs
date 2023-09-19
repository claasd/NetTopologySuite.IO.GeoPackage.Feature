using Dapper;
using Microsoft.Data.Sqlite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;

namespace CdIts.NetTopologySuite.GeoPackageFeatureReader;

public class GeoPackage : IDisposable
{
    private readonly SqliteConnection _conn;

    public GeoPackage(string path)
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        _conn = new SqliteConnection($"Data Source={path}");
        _conn.Open();
    }

    public IList<GeoPackageFeatureInfo> GetFeatureInfos()
    {
        return _conn.Query<GeoPackageFeatureInfo>("SELECT * FROM gpkg_contents WHERE data_type = 'features'").AsList();
    }

    public IList<GeoPackageSpatialReference> GetSpatialReferenceSystems()
    {
        return _conn.Query<GeoPackageSpatialReference>("SELECT * FROM gpkg_spatial_ref_sys").AsList();
    }

    public Feature[] ReadFeatures(string tableName)
    {
        var geoColumn = _conn.QuerySingle<string>("SELECT column_name FROM gpkg_geometry_columns WHERE table_name = @tableName", new { tableName });
        var reader = new GeoPackageGeoReader();
        var lines = _conn.Query($@"SELECT * FROM ""{tableName}""");
        return lines.Select(data =>
        {
            var line = data as IDictionary<string, object>;
            var geoBytes = line[geoColumn] as byte[];
            var geo = reader.Read(geoBytes);
            var attributes = line.Keys.Where(k => k != geoColumn).ToDictionary(k => k, k => line[k]);
            return new Feature(geo, new AttributesTable(attributes));
        }).ToArray();
    }

    public static IList<GeoPackageFeatureLayer> ReadGeoPackage(string path)
    {
        var result = new List<GeoPackageFeatureLayer>();
        var package = new GeoPackage(path);
        var infos = package.GetFeatureInfos();
        var srs = package.GetSpatialReferenceSystems().ToLookup(item=>item.SrsId);
        foreach (var featureInfo in infos)
        {
            var features = package.ReadFeatures(featureInfo.TableName);
            var layer = new GeoPackageFeatureLayer(featureInfo, features, srs[featureInfo.SrsId].FirstOrDefault());
            result.Add(layer);
        }
        return result;
    }

    public void Dispose()
    {
        _conn.Dispose();
    }
}