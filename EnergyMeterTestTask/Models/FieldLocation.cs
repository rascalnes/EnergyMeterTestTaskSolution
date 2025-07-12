using System.Security.Cryptography;

namespace EnergyMeterTestTask.Models
{
    public class FieldLocation
    {
        public GeoPoint Center { get; set; }
        public List<GeoPoint> Polygon { get; set; }
    }
}
