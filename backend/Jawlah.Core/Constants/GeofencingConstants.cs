namespace Jawlah.Core.Constants;

// geofencing constants for location validation
public static class GeofencingConstants
{
    // palestine region bounds
    public const double MinLatitude = 31.0;
    public const double MaxLatitude = 33.5;
    public const double MinLongitude = 34.0;
    public const double MaxLongitude = 36.0;

    // geofencing buffer tolerance in degrees (~30 meters)
    public const double BufferToleranceDegrees = 0.0003;

    // maximum acceptable gps accuracy in meters
    public const double MaxAcceptableAccuracyMeters = 50.0;
}
