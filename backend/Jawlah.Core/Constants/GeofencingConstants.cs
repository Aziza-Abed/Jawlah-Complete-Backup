namespace Jawlah.Core.Constants;

// geofencing constants for location validation
// NOTE: Municipality-specific bounds are now stored in the Municipalities table
// These are fallback/default values for backward compatibility
public static class GeofencingConstants
{
    // Default bounds - Al-Bireh municipality (for backward compatibility)
    // In multi-municipality mode, these should be replaced with municipality-specific bounds
    public const double MinLatitude = 31.87;   // Southern boundary
    public const double MaxLatitude = 31.95;   // Northern boundary
    public const double MinLongitude = 35.18;  // Western boundary
    public const double MaxLongitude = 35.27;  // Eastern boundary

    // Extended bounds for testing/demo mode (includes Birzeit University area)
    public const double TestingMinLatitude = 31.85;   // Extended south
    public const double TestingMaxLatitude = 31.98;   // Extended north (includes Birzeit ~31.956)
    public const double TestingMinLongitude = 35.15;  // Extended west
    public const double TestingMaxLongitude = 35.30;  // Extended east

    // geofencing buffer tolerance in degrees (~30 meters)
    public const double BufferToleranceDegrees = 0.0003;

    // proof location tolerance in degrees (~100 meters)
    // larger than check-in tolerance since worker may be at edge of zone
    public const double ProofLocationToleranceDegrees = 0.001;

    // maximum acceptable gps accuracy in meters
    // Note: 50m is ideal for production, 150m allows indoor testing
    // This can be overridden per-municipality in the Municipalities table
    public const double MaxAcceptableAccuracyMeters = 150.0;

    // Alias for consistency
    public const double DefaultMaxAcceptableAccuracyMeters = MaxAcceptableAccuracyMeters;

    // Global bounds for Palestine region (for basic sanity checks)
    // Actual municipality-specific bounds are in the database
    public const double PalestineMinLatitude = 29.5;   // Southern boundary
    public const double PalestineMaxLatitude = 33.5;   // Northern boundary
    public const double PalestineMinLongitude = 34.0;  // Western boundary
    public const double PalestineMaxLongitude = 36.0;  // Eastern boundary

    /// <summary>
    /// Check if coordinates are within the general Palestine region
    /// For more specific validation, use the municipality's bounding box
    /// </summary>
    public static bool IsWithinPalestine(double latitude, double longitude)
    {
        return latitude >= PalestineMinLatitude && latitude <= PalestineMaxLatitude &&
               longitude >= PalestineMinLongitude && longitude <= PalestineMaxLongitude;
    }
}
