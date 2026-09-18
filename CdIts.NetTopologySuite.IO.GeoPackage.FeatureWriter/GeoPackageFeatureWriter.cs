using System.Reflection;
using CdIts.NetTopologySuite.IO.GeoPackage.Features;
using Dapper;
using Microsoft.Data.Sqlite;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter;

public class GeoPackageFeatureWriter : IDisposable, IAsyncDisposable
{
    public SqliteConnection Connection { get; }
    private readonly List<int> _srsIds = [4326];

    public enum Types
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
                Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("CdIts.NetTopologySuite.IO.GeoPackage.FeatureWriter.template.gpkg")!;
            source.CopyTo(stream);
        }

        Connection = new SqliteConnection($"Data Source={path}");
        Connection.Open();
    }

    public async Task CloseAsync()
    {
        SqliteConnection.ClearPool(Connection);
        await Connection.CloseAsync();
    }

    public void Close()
    {
        SqliteConnection.ClearPool(Connection);
        Connection.Close();
    }

    public void Dispose()
    {
        SqliteConnection.ClearPool(Connection);
        Connection.Dispose();
    }


    public async ValueTask DisposeAsync()
    {
        SqliteConnection.ClearPool(Connection);
        await Connection.DisposeAsync();
    }

    public async Task AddSrsAsync(int id, string name, string definition, string organization,
        int? organizationId = null, string description = "")
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
        await Connection.ExecuteAsync(
            "INSERT INTO gpkg_spatial_ref_sys (srs_id, srs_name, organization, organization_coordsys_id, definition, description) VALUES (@SrsId, @SrsName, @Organization, @OrganizationCoordsysId, @Definition, @Description)",
            srs);
        _srsIds.Add(srs.SrsId);
    }

    public void AddSrs(int id, string name, string definition, string organization, int? organizationId = null,
        string description = "") =>
        AddSrsAsync(id, name, definition, organization, organizationId, description).Wait();

    public Task<GeoPackageFeatureInfo> AddLayerAsync(ICollection<Feature> features, string layerName, int srsId = 4326,
        string geometryFieldName = "geometry",
        string idFieldName = "id")
    {
        if (features.Count == 0)
            throw new ArgumentException("need at least one feature to add a layer", nameof(features));

        var firstFeature = features.First();
        var fieldNames = ConvertFieldNames(firstFeature.Attributes);
        return AddLayerAsync(firstFeature.Geometry.OgcGeometryType, fieldNames, features, layerName, srsId,
            geometryFieldName, idFieldName, !double.IsNaN(firstFeature.Geometry?.Coordinate?.Z ?? double.NaN),
            !double.IsNaN(firstFeature.Geometry?.Coordinate?.M ?? double.NaN));
    }

    public async Task<GeoPackageFeatureInfo> AddLayerAsync(OgcGeometryType geometryType, Dictionary<string, Types> fields,
        ICollection<Feature> features, string layerName, int srsId = 4326, string geometryFieldName = "geometry",
        string idFieldName = "id", bool hasZ = false, bool hasM = false)
    {
        if (!_srsIds.Contains(srsId))
            throw new ArgumentException("srsId must be registered before using it", nameof(srsId));

        var idField = fields.Keys.FirstOrDefault(p => p.Equals(idFieldName, StringComparison.OrdinalIgnoreCase));
        if (idField is null)
            throw new ArgumentException(
                $"attributes must contain a filed named '{idFieldName}' that will be used as primary key",
                nameof(features));
        var idType = fields[idField];
        fields.Remove(idField);

        var layerWriter = new GeoPackageLayerWriter(Connection, srsId, layerName, idField, geometryFieldName, fields);
        await layerWriter.CreateTableAsync(idType, geometryType);
        var bbox = await layerWriter.WriteFeaturesAsync(features);
        var info = await layerWriter.UpdateContentsTable(bbox);
        info.GeometryInfo = await layerWriter.RegisterColumns(geometryType, hasZ, hasM);
        return info;
    }

    public GeoPackageFeatureInfo AddLayer(ICollection<Feature> features, string layerName, int srsId = 4326,
        string geometryFieldName = "geometry")
        => AddLayerAsync(features, layerName, srsId, geometryFieldName).GetAwaiter().GetResult();

    
    
    private Types GetFieldType(Type type)
    {
        if (type == typeof(int) || type == typeof(short) || type == typeof(ushort) || type == typeof(uint) ||
            type == typeof(long) ||
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