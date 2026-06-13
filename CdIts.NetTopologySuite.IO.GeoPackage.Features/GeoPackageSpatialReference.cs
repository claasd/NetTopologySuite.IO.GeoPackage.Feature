namespace CdIts.NetTopologySuite.IO.GeoPackage.Features;

public class GeoPackageSpatialReference
{
    public int SrsId { get; set; }
    public string SrsName { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public int OrganizationCoordsysId { get; set; }
    public string Definition { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}