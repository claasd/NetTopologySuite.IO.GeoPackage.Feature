using System.Globalization;
using System.Text;
using CdIts.NetTopologySuite.IO.GeoPackage.Features;
using Dapper;
using Microsoft.Data.Sqlite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter;

internal class GeoPackageLayerWriter
{
    private readonly SqliteConnection _conn;
    private readonly string _layerName;
    private readonly string _idField;
    private readonly string _geometryFieldName;
    private readonly Dictionary<string, GeoPackageFeatureWriter.Types> _fieldNames;
    private readonly int _srsId;
    private readonly GeoPackageGeoWriter _writer = new();
    internal GeoPackageLayerWriter(SqliteConnection conn, int srsId, string layerName, string idField, string geometryFieldName,
        Dictionary<string, GeoPackageFeatureWriter.Types> fieldNames)
    {
        _conn = conn;
        _srsId = srsId;
        _layerName = layerName;
        _idField = idField;
        _geometryFieldName = geometryFieldName;
        _fieldNames = fieldNames;
    }

    internal async Task CreateTableAsync(GeoPackageFeatureWriter.Types idType, OgcGeometryType geometryType)
    {
        var createTable =
            new StringBuilder(
                $"CREATE TABLE {QuoteIdentifier(_layerName)} ({QuoteIdentifier(_idField)} {idType.ToString().ToUpper()} PRIMARY KEY, {QuoteIdentifier(_geometryFieldName)} {geometryType.ToString().ToUpper()}");
        foreach (var (name, type) in _fieldNames)
        {
            createTable.Append($", {QuoteIdentifier(name)} {type.ToString().ToUpper()}");
        }

        createTable.Append(')');
        await _conn.ExecuteAsync(createTable.ToString()).ConfigureAwait(false);
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";


    internal async Task<GeoPackageGeometryInfo> RegisterColumns(OgcGeometryType geometryType, bool hasZ = false, bool hasM = false)
    {
        await _conn.ExecuteAsync(
            "INSERT INTO gpkg_geometry_columns (table_name, column_name, geometry_type_name, srs_id, z, m) VALUES (@TableName, @GeometryFieldName, @GeometryType, @SrsId, @HasZ, @HasM)",
            new
            {
                TableName = _layerName, GeometryFieldName = _geometryFieldName, GeometryType = geometryType.ToString().ToUpper(), SrsId = _srsId,
                HasZ = hasZ, HasM = hasM
            }).ConfigureAwait(false);
        return new GeoPackageGeometryInfo
        {
            TableName = _layerName, ColumnName = _geometryFieldName, GeometryTypeName = geometryType.ToString().ToUpper(), SrsId = _srsId, Z = hasZ, M = hasM
        };
    }

    internal async Task<Envelope> WriteFeaturesAsync(ICollection<Feature> features)
    {
        var insert = CreateInsertStatement();
        var bbox = new Envelope();
        var parameters = new List<DynamicParameters>(features.Count);
        foreach (var feature in features)
        {
            bbox = bbox.ExpandedBy(feature.BoundingBox ?? feature.Geometry.EnvelopeInternal);
            parameters.Add(ToSqlInsertData(feature));
        }
        var transaction = _conn.BeginTransaction();
        try
        {
            await _conn.ExecuteAsync(insert, parameters, transaction).ConfigureAwait(false);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            transaction.Dispose();
        }
        return bbox;
    }

    private DynamicParameters ToSqlInsertData(Feature feature)
    {
        
        var data = _writer.Write(feature.Geometry);
        var parameters = new DynamicParameters(new { Geometry = data, Id = feature.Attributes[_idField] });
        var index = 1;

        foreach (var (name, type) in _fieldNames)
        {
            var value = feature.Attributes.GetOptionalValue(name);
            if (type == GeoPackageFeatureWriter.Types.Text && value != null)
                value = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (value is DateTime dt)
                value = dt.ToString("O");
            if (value is DateTimeOffset dto)
                value = dto.ToString("O");
            parameters.Add($"Data{index}", value);
            index++;
        }

        return parameters;
    }

    private string CreateInsertStatement()
    {
        var insert = new StringBuilder($"INSERT INTO {QuoteIdentifier(_layerName)} ({QuoteIdentifier(_idField)}, {QuoteIdentifier(_geometryFieldName)}");
        var insertValues = new StringBuilder($"(@Id, @Geometry");
        var index = 1;
        foreach (var name in _fieldNames.Keys)
        {
            insert.Append($", {QuoteIdentifier(name)}");
            insertValues.Append($", @Data{index}");
            index++;
        }

        insert.Append(") VALUES ").Append(insertValues.ToString()).Append(')');
        return insert.ToString();
    }

    public async Task<GeoPackageFeatureInfo> UpdateContentsTable(Envelope bbox)
    {
        await _conn.ExecuteAsync(
            "INSERT INTO gpkg_contents (table_name, data_type, identifier, srs_id, min_x, min_y, max_x, max_y) VALUES (@TableName, 'features', @TableName, @SrsId, @MinX, @MinY, @MaxX, @MaxY)",
            new { TableName = _layerName, SrsId = _srsId, bbox.MinX, bbox.MinY, bbox.MaxX, bbox.MaxY }).ConfigureAwait(false);
        return new GeoPackageFeatureInfo
        {
            Identifier = _layerName,
            SrsId = _srsId,
            TableName = _layerName,
            MinX = bbox.MinX,
            MaxX = bbox.MaxX,
            MinY = bbox.MinY,
            MaxY = bbox.MaxY
        };
    }
}