using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Tests;

public class TestDataFactory
{
    public static List<Feature> CreateLineFeature()
    {
        var lines = new List<Feature>();
        for (var i = 0; i < 10; i++)
        {
            var x = i/10.0 + 10.0;
            var line = new Feature(new LineString(new[]
            {
                
                new Coordinate(x+0.01, 50.0),
                new Coordinate(x+0.03, 50.1),
                new Coordinate(x+0.05, 50.2),
                new Coordinate(x+0.07, 50.3),
                new Coordinate(x+0.09, 50.4)
            }), new AttributesTable
            {
                {"ID", i},
                {"date", DateTime.Now},
                {"internalID", $"X{i}"},
                {"testDouble", x /13.1}
            });
            lines.Add(line);
        }

        return lines;
    }
}