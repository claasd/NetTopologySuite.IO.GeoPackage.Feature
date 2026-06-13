namespace CdIts.NetTopologySuite.IO.GeoPackage.Features;

public class GeoPackageGeometryInfo
{
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string GeometryTypeName { get; set; } = string.Empty;
    public int SrsId { get; set; }
    public bool Z { get; set; }
    public bool M { get; set; }
}