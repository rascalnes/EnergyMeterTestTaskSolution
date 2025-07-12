using EnergyMeterTestTask.Services;
using EnergyMeterTestTask.Models;
using Microsoft.AspNetCore.Mvc;

namespace EnergyMeterTestTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FieldsController : ControllerBase
    {
        private readonly IFieldService _fieldService;

        public FieldsController(IFieldService fieldService)
        {
            _fieldService = fieldService;
        }

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Field>), 200)]
        public IActionResult GetAllFields() => Ok(_fieldService.GetAllFieldsAsync());

        [HttpGet("{id}/size")]
        [ProducesResponseType(typeof(double), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetFieldSize(string id)
        {
            var size = await _fieldService.GetFieldSizeAsync(id);
            return size.HasValue ? Ok(size.Value) : NotFound();
        }

        [HttpGet("{id}/distance")]
        [ProducesResponseType(typeof(double), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> CalculateDistance(string id, [FromQuery] double lat, [FromQuery] double lng)
        {
            var distance = await _fieldService.CalculateDistanceToCenterAsync(id, new GeoPoint(lat, lng));
            return distance.HasValue ? Ok(distance.Value) : NotFound();
        }

        [HttpGet("check-point")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> CheckPointInFields([FromQuery] double lat, [FromQuery] double lng)
        {
            var result = await _fieldService.CheckPointInFieldsAsync(new GeoPoint(lat, lng));
            return result.HasValue
                ? Ok(new { result.Value.id, result.Value.name })
                : Ok(false);
        }
    }
}
