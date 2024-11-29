using CdIts.NetTopologySuite.IO.GeoPackage.Features;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NetTopologySuite.Features;
using NetTopologySuite.IO;

namespace CdIts.NetTopologySuite.IO.GeoPackage.FeatureReader;

public class GeoPackageFeatureReader : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly ILogger _logger;
    private readonly bool _failOnInvalidShapes;

    public GeoPackageFeatureReader(string path) : this(path, false, null)
    {
    }

    public GeoPackageFeatureReader(string path, bool failOnInvalidShapes, ILogger? logger = null)
    {
        _logger = logger ?? NullLogger.Instance;
        _failOnInvalidShapes = failOnInvalidShapes;
        DefaultTypeMap.MatchNamesWithUnderscores = true;
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
            try
            {
                var line = data as IDictionary<string, object>;
                var geoBytes = line[geoColumn] as byte[];
                if (geoBytes is null)
                    throw new ArgumentNullException(geoColumn, "Geometry column is null");
                var geo = reader.Read(geoBytes);
                var attributes = line.Keys.Where(k => k != geoColumn).ToDictionary(k => k, k => line[k]);
                return new Feature(geo, new AttributesTable(attributes));
            }
            catch (Exception e)
            {
                _logger.LogWarning("Error: {Message} in feature '{TableName}'", e.Message, tableName);
                if (_failOnInvalidShapes)
                    throw;
                return null;
            }
        }).Where(f => f != null).ToArray();
    }


    public static IList<GeoPackageFeatureLayer> ReadGeoPackage(string path) => ReadGeoPackage(path, false);

    public static IList<GeoPackageFeatureLayer> ReadGeoPackage(string path, bool failOnInvalidShapes, ILogger? logger = null)
    {
        var result = new List<GeoPackageFeatureLayer>();
        using var package = new GeoPackageFeatureReader(path, failOnInvalidShapes, logger);
        var infos = package.GetFeatureInfos();
        var srs = package.GetSpatialReferenceSystems().ToLookup(item => item.SrsId);
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
        SqliteConnection.ClearPool(_conn);
        _conn.Dispose();
    }
}