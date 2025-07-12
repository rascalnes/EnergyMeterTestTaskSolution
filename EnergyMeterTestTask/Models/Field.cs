namespace EnergyMeterTestTask.Models
{
    public class Field
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public double Size { get; set; }
        public FieldLocation Locations { get; set; }
    }
}
