using EnergyMeterTestTask.Models;

namespace EnergyMeterTestTask.Services
{
    public interface IFieldService
    {
        IEnumerable<Field> GetAllFields();
        Field GetFieldById(string id);
        double? GetFieldSize(string id);
        double? CalculateDistanceToCenter(string fieldId, Point point);
        (string id, string name)? CheckPointInFields(Point point);
    }
}
