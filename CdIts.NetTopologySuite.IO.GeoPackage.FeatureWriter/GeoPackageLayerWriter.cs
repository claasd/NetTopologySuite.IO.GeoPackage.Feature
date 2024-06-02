using System.Globalization;
using System.Text;
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

    internal async Task CreateTableAsync(string geometryType)
    {
        var createTable =
            new StringBuilder(
                $@"CREATE TABLE  ""{_layerName}"" (""{_idField}"" INTEGER PRIMARY KEY, ""{_geometryFieldName}"" {geometryType.ToUpper()}");
        foreach (var (name, type) in _fieldNames)
        {
            createTable.Append($@", ""{name}"" {type.ToString().ToUpper()}");
        }

        createTable.Append(')');
        await _conn.ExecuteAsync(createTable.ToString());
    }


    internal async Task RegisterColumns(string geometryType, bool hasZ = false, bool hasM = false)
    {
        await _conn.ExecuteAsync(
            "INSERT INTO gpkg_geometry_columns (table_name, column_name, geometry_type_name, srs_id, z, m) VALUES (@TableName, @GeometryFieldName, @GeometryType, @SrsId, @HasZ, @HasM)",
            new
            {
                TableName = _layerName, GeometryFieldName = _geometryFieldName, GeometryType = geometryType.ToUpper(), SrsId = _srsId,
                HasZ = hasZ, HasM = hasM
            });
    }

    internal async Task<Envelope> WriteFeaturesAsync(ICollection<Feature> features)
    {
        var insert = CreateInsertStatement();
        var bbox = new Envelope();
        foreach (var feature in features)
        {
            bbox = bbox.ExpandedBy(feature.BoundingBox ?? feature.Geometry.EnvelopeInternal);
            var parameters = ToSqlInsertData(feature);
            await _conn.ExecuteAsync(insert, parameters);
        }

        return bbox;
    }

    private DynamicParameters ToSqlInsertData(Feature feature)
    {
        var writer = new GeoPackageGeoWriter();
        var data = writer.Write(feature.Geometry);
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
        var insert = new StringBuilder($@"INSERT INTO ""{_layerName}"" (""{_idField}"", ""{_geometryFieldName}""");
        var insertValues = new StringBuilder($"(@Id, @Geometry");
        var index = 1;
        foreach (var name in _fieldNames.Keys)
        {
            insert.Append($@", ""{name}""");
            insertValues.Append($", @Data{index}");
            index++;
        }

        insert.Append(") VALUES ").Append(insertValues.ToString()).Append(')');
        return insert.ToString();
    }

    public async Task UpdateContentsTable(Envelope bbox)
    {
        await _conn.ExecuteAsync(
            "INSERT INTO gpkg_contents (table_name, data_type, identifier, srs_id, min_x, min_y, max_x, max_y) VALUES (@TableName, 'features', @TableName, @SrsId, @MinX, @MinY, @MaxX, @MaxY)",
            new { TableName = _layerName, SrsId = _srsId, bbox.MinX, bbox.MinY, bbox.MaxX, bbox.MaxY });
    }
}