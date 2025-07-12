namespace EnergyMeterTestTask.Models
{
    public class GeoPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public GeoPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
