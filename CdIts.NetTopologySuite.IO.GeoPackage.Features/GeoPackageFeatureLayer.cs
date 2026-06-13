using NetTopologySuite.Features;

namespace CdIts.NetTopologySuite.IO.GeoPackage.Features;

public class GeoPackageFeatureLayer(GeoPackageFeatureInfo info, Feature[] features, GeoPackageSpatialReference? geoPackageSpatialReference)
{
    public GeoPackageFeatureInfo Info { get; } = info;
    public Feature[] Features { get; } = features;
    public GeoPackageSpatialReference? GeoPackageSpatialReference { get; } = geoPackageSpatialReference;
}
