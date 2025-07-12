using EnergyMeterTestTask.Models;

namespace EnergyMeterTestTask.Services
{
    public interface IFieldService
    {
        IEnumerable<Field> GetAllFieldsAsync();
        Task<Field> GetFieldByIdAsync(string id);
        Task<double?> GetFieldSizeAsync(string id);
        Task<double?> CalculateDistanceToCenterAsync(string fieldId, GeoPoint point);
        Task<(string id, string name)?> CheckPointInFieldsAsync(GeoPoint point);
    }
}
