namespace AriaHR.Modules.Attendance.Application.Geofencing;

/// <summary>
/// Reliable great-circle distance calculation used for server-side geofence validation.
/// </summary>
public static class GeoDistanceCalculator
{
    private const double EarthRadiusMeters = 6371000d;

    public static bool IsValidLatitude(double latitude) => latitude >= -90d && latitude <= 90d;

    public static bool IsValidLongitude(double longitude) => longitude >= -180d && longitude <= 180d;

    /// <summary>
    /// Haversine distance in metres between two WGS-84 coordinates.
    /// </summary>
    public static double CalculateDistanceInMeters(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        double lat1 = ToRadians(latitude1);
        double lat2 = ToRadians(latitude2);
        double deltaLat = ToRadians(latitude2 - latitude1);
        double deltaLon = ToRadians(longitude2 - longitude1);

        double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * (Math.PI / 180d);
}
