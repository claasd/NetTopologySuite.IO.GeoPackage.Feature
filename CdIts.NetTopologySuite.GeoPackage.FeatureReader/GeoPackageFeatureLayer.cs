using NetTopologySuite.Features;

namespace CdIts.NetTopologySuite.GeoPackage.FeatureReader;

public class GeoPackageFeatureLayer
{
    public GeoPackageFeatureLayer(GeoPackageFeatureInfo info, Feature[] features, GeoPackageSpatialReference? geoPackageSpatialReference)
    {
        Info = info;
        Features = features;
        GeoPackageSpatialReference = geoPackageSpatialReference;
    }
    public GeoPackageFeatureInfo Info { get; }
    public Feature[] Features { get; }
    public GeoPackageSpatialReference? GeoPackageSpatialReference { get; }
}
