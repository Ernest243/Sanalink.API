using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanalink.API.DTOs;
using Sanalink.API.Services;
using System.Security.Claims;

namespace Sanalink.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class EncounterController : ControllerBase
    {
        private readonly IEncounterService _encounterService;

        public EncounterController(IEncounterService encounterService)
        {
            _encounterService = encounterService;
        }

        [HttpGet("my")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyEncounters()
        {
            var patientIdClaim = User.FindFirstValue("patientId");
            if (!int.TryParse(patientIdClaim, out int patientId))
                return Unauthorized();

            var encounters = await _encounterService.GetEncountersByPatientAsync(patientId);
            return Ok(encounters);
        }

        [HttpGet]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetAllEncounters()
        {
            var encounters = await _encounterService.GetAllEncountersAsync();
            return Ok(encounters);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetEncounterById(int id)
        {
            var encounter = await _encounterService.GetEncounterByIdAsync(id);
            if (encounter == null) return NotFound();
            return Ok(encounter);
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetEncountersByPatient(int patientId)
        {
            var encounters = await _encounterService.GetEncountersByPatientAsync(patientId);
            return Ok(encounters);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreateEncounter([FromBody] EncounterCreateDto dto)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(doctorId))
                return Unauthorized();

            var result = await _encounterService.CreateEncounterAsync(dto, doctorId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Nurse")]
        public async Task<IActionResult> UpdateEncounter(int id, [FromBody] EncounterUpdateDto dto)
        {
            var result = await _encounterService.UpdateEncounterAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            var success = await _encounterService.UpdateStatusAsync(id, newStatus);
            if (!success) return BadRequest("Invalid status transition.");
            return Ok();
        }
    }
}
