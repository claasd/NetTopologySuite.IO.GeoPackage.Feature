namespace CdIts.NetTopologySuite.IO.GeoPackage.Features;

public class GeoPackageSpatialReference
{
    public int SrsId { get; set; }
    public string SrsName { get; set; }
    public string Organization { get; set; }
    public int OrganizationCoordsysId { get; set; }
    public string Definition { get; set; }
    public string Description { get; set; }
}