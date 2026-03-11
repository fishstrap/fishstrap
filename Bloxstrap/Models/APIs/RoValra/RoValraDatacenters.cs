using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LocationDataModels
{
    public class RoValraDatacenter
    {
        [JsonPropertyName("location")]
        public RoValraDatacenterLocation Location { get; set; }
    }

    public class RoValraDatacenterLocation
    {
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;

        // This captures the raw JSON array
        [JsonPropertyName("latLong")]
        public string[] RawLatLong { get; set; }

        // Expose Latitude as a distinct, usable double
        [JsonIgnore]
        public double Latitude
        {
            get
            {
                if (RawLatLong != null && RawLatLong.Length > 0 && double.TryParse(RawLatLong[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double lat))
                {
                    return lat;
                }
                return 0; // Or handle missing data as you prefer
            }
        }

        // Expose Longitude as a distinct, usable double
        [JsonIgnore]
        public double Longitude
        {
            get
            {
                if (RawLatLong != null && RawLatLong.Length > 1 && double.TryParse(RawLatLong[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double lon))
                {
                    return lon;
                }
                return 0; // Or handle missing data as you prefer
            }
        }
    }
}