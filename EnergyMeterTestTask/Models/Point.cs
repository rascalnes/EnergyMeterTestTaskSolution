namespace EnergyMeterTestTask.Models
{
    public class Point
    {
        public double Lat { get; set; }
        public double Lng { get; set; }

        public Point(double lat, double lng)
        {
            Lat = lat;
            Lng = lng;
        }
    }
}
