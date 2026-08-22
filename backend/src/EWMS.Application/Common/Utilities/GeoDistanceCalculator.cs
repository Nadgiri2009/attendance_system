namespace EWMS.Application.Common.Utilities;

// Single shared implementation of the Haversine great-circle distance
// formula, used by StopTrackingSessionCommandHandler (total distance
// travelled) and available to any future GPS/geofence feature that needs
// point-to-point distance, so it isn't reimplemented per-handler.
public static class GeoDistanceCalculator
{
    private const double EarthRadiusMeters = 6_371_000d;

    public static double CalculateMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusMeters * c;
    }

    // Sums point-to-point distance across an ordered path (start point first).
    public static double TotalPathDistanceMeters(IReadOnlyList<(double Lat, double Lon)> orderedPoints)
    {
        if (orderedPoints.Count < 2) return 0;

        double total = 0;
        for (var i = 1; i < orderedPoints.Count; i++)
        {
            var (lat1, lon1) = orderedPoints[i - 1];
            var (lat2, lon2) = orderedPoints[i];
            total += CalculateMeters(lat1, lon1, lat2, lon2);
        }
        return total;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
