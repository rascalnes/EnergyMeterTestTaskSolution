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
        public IActionResult GetAllFields()
        {
            var fields = _fieldService.GetAllFields();
            return Ok(fields);
        }

        [HttpGet("{id}/size")]
        public IActionResult GetFieldSize(string id)
        {
            var size = _fieldService.GetFieldSize(id);
            if (size == null) return NotFound();
            return Ok(size);
        }

        [HttpGet("{id}/distance")]
        public IActionResult CalculateDistance(string id, [FromQuery] double lat, [FromQuery] double lng)
        {
            var distance = _fieldService.CalculateDistanceToCenter(id, new Point(lat, lng));
            if (distance == null) return NotFound();
            return Ok(distance);
        }

        [HttpGet("check-point")]
        public IActionResult CheckPointInFields([FromQuery] double lat, [FromQuery] double lng)
        {
            var result = _fieldService.CheckPointInFields(new Point(lat, lng));
            if (result == null) return Ok(false);
            return Ok(new { result.Value.id, result.Value.name });
        }
    }
}
