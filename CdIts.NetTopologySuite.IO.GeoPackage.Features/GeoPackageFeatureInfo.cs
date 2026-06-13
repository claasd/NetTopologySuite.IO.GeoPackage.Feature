using NetTopologySuite.Geometries;

namespace CdIts.NetTopologySuite.IO.GeoPackage.Features;

public class GeoPackageFeatureInfo
{
    public string TableName { get; set; } = string.Empty;
    public string Identifier { get; set; } = string.Empty;
    public string DataType => "features";
    public string Description { get; set; } = string.Empty;
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }
    public Envelope Envelope() => new(MinX, MaxX, MinY, MaxY);
    public int SrsId { get; set; }
    public GeoPackageGeometryInfo? GeometryInfo { get; set; }
}