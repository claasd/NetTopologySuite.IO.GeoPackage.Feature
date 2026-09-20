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
    public SqliteConnection Connection { get; }
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
        Connection = new SqliteConnection($"Data Source={path}");
        Connection.Open();
    }

    public IList<GeoPackageFeatureInfo> GetFeatureInfos()
    {
        var contents = Connection.Query<GeoPackageFeatureInfo>("SELECT * FROM gpkg_contents WHERE data_type = 'features'").AsList();
        var geoInfo = Connection.Query<GeoPackageGeometryInfo>("SELECT * FROM gpkg_geometry_columns").AsList();
        foreach (var info in contents)
        {
            info.GeometryInfo = geoInfo.FirstOrDefault(x => x.TableName == info.TableName);
        }
        return contents;
    }

    public IList<GeoPackageSpatialReference> GetSpatialReferenceSystems() => Connection.Query<GeoPackageSpatialReference>("SELECT * FROM gpkg_spatial_ref_sys").AsList();

    public Feature[] ReadFeatures(string tableName)
    {
        var geoColumn = Connection.QuerySingle<string>("SELECT column_name FROM gpkg_geometry_columns WHERE table_name = @tableName", new { tableName });
        var reader = new GeoPackageGeoReader();
        var lines = Connection.Query($"SELECT * FROM {QuoteIdentifier(tableName)}");
        var rowIndex = 0;
        return lines.Select(data =>
        {
            var currentRow = rowIndex++;
            try
            {
                if (data is not IDictionary<string, object> line)
                {
                    throw new InvalidDataException($"Row {currentRow} could not be read");
                }
                if (line[geoColumn] is null)
                {
                    throw new InvalidDataException($"Geometry column '{geoColumn}' is null");
                }
                if (line[geoColumn] is not byte[] geoBytes)
                {
                    throw new InvalidDataException($"Geometry column '{geoColumn}' does not contain a valid geometry blob");
                }
                var geo = reader.Read(geoBytes);
                var attributes = line.Keys.Where(k => k != geoColumn).ToDictionary(k => k, k => line[k]);
                return new Feature(geo, new AttributesTable(attributes));
            }
            catch (Exception e)
            {
                _logger.LogWarning("Error: {Message} in feature {RowIndex} of table '{TableName}'", e.Message, currentRow, tableName);
                if (_failOnInvalidShapes)
                    throw;
                return null;
            }
        }).OfType<Feature>().ToArray();
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";


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
        SqliteConnection.ClearPool(Connection);
        Connection.Dispose();
    }
}