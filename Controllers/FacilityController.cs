using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanalink.API.DTOs;
using Sanalink.API.Services;

namespace Sanalink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FacilityController : ControllerBase
    {
        private readonly IFacilityService _facilityService;

        public FacilityController(IFacilityService facilityService)
        {
            _facilityService = facilityService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllFacilities()
        {
            var facilities = await _facilityService.GetAllFacilitiesAsync();
            return Ok(facilities);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetFacilityById(int id)
        {
            var facility = await _facilityService.GetFacilityByIdAsync(id);
            if (facility == null) return NotFound();
            return Ok(facility);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateFacility([FromBody] FacilityCreateDto dto)
        {
            var result = await _facilityService.CreateFacilityAsync(dto);
            return Ok(result);
        }
    }
}
