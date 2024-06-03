using System.Reflection;
using CdIts.NetTopologySuite.IO.GeoPackage.Features;
using Dapper;
using Microsoft.Data.Sqlite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter;

public class GeoPackageFeatureWriter : IDisposable, IAsyncDisposable
{
    private readonly SqliteConnection _conn;
    private readonly List<int> _srsIds = new() { 4326 };

    internal enum Types
    {
        Integer,
        Real,
        Text,
        Blob,
        DateTime
    }

    public GeoPackageFeatureWriter(string path)
    {
        using (var stream = new FileStream(path, FileMode.Create))
        {
            var source =
                Assembly.GetExecutingAssembly().GetManifestResourceStream("CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter.template.gpkg")!;
            source.CopyTo(stream);
        }

        _conn = new SqliteConnection($"Data Source={path}");
        _conn.Open();
    }

    public async Task CloseAsync(bool clearAllPools = false)
    {
        await _conn.CloseAsync();
        if(clearAllPools)
            SqliteConnection.ClearAllPools();
    }

    public void Close(bool clearAllPools = false)
    {
        _conn.Close();
        if(clearAllPools)
            SqliteConnection.ClearAllPools();
    }

    public void Dispose()
    {
        _conn.Dispose();
    }


    public async ValueTask DisposeAsync()
    {
        await _conn.DisposeAsync();
    }

    public async Task AddSrsAsync(int id, string name, string definition, string organization, int? organizationId = null, string description = "")
    {
        var srs = new GeoPackageSpatialReference
        {
            SrsId = id,
            Definition = definition,
            Description = description,
            Organization = organization,
            SrsName = name,
            OrganizationCoordsysId = organizationId ?? id
        };
        await _conn.ExecuteAsync(
            "INSERT INTO gpkg_spatial_ref_sys (srs_id, srs_name, organization, organization_coordsys_id, definition, description) VALUES (@SrsId, @SrsName, @Organization, @OrganizationCoordsysId, @Definition, @Description)",
            srs);
        _srsIds.Add(srs.SrsId);
    }

    public void AddSrs(int id, string name, string definition, string organization, int? organizationId = null, string description = "") =>
        AddSrsAsync(id, name, definition, organization, organizationId, description).Wait();

    public async Task AddLayerAsync(ICollection<Feature> features, string layerName, int srsId = 4326, string geometryFieldName = "geometry",
        string idFieldName = "id")
    {
        if (features.Count == 0)
            throw new ArgumentException("need at least one feature to add a layer", nameof(features));
        if (!_srsIds.Contains(srsId))
            throw new ArgumentException("srsId must be registered before using it", nameof(srsId));
        var firstFeature = features.First();
        var fieldNames = ConvertFieldNames(firstFeature.Attributes);
        var idField = fieldNames.Keys.FirstOrDefault(p => p.Equals(idFieldName, StringComparison.InvariantCulture));
        if (idField is null)
            throw new ArgumentException($"attributes must contain a filed named '{idFieldName}' that will be used as primary key", nameof(features));
        var idType = fieldNames[idField];
        fieldNames.Remove(idField);

        var layerWriter = new GeoPackageLayerWriter(_conn, srsId, layerName, idField, geometryFieldName, fieldNames);
        await layerWriter.CreateTableAsync(idType, firstFeature.Geometry.GeometryType);
        var bbox = await layerWriter.WriteFeaturesAsync(features);
        await layerWriter.UpdateContentsTable(bbox);
        await layerWriter.RegisterColumns(firstFeature.Geometry.GeometryType, !double.IsNaN(firstFeature.Geometry.Coordinate.Z),
            !double.IsNaN(firstFeature.Geometry.Coordinate.M));
    }

    public void AddLayer(ICollection<Feature> features, string layerName, int srsId = 4326, string geometryFieldName = "geometry")
        => AddLayerAsync(features, layerName, srsId, geometryFieldName).Wait();
    private Types GetFieldType(Type type)
    {
        if (type == typeof(int) || type == typeof(short) || type == typeof(ushort) || type == typeof(uint) || type == typeof(long) ||
            type == typeof(ulong) || type == typeof(byte) || type == typeof(sbyte))
            return Types.Integer;
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return Types.Real;
        if (type == typeof(byte[]))
            return Types.Blob;
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return Types.DateTime;
        return Types.Text;
    }

    private Dictionary<string, Types> ConvertFieldNames(IAttributesTable featureAttributes) =>
        featureAttributes.GetNames().ToDictionary(name => name, name => GetFieldType(featureAttributes.GetType(name)));
}