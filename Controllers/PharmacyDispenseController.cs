using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanalink.API.DTOs;
using Sanalink.API.Services;
using System.Security.Claims;

namespace Sanalink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PharmacyDispenseController : ControllerBase
    {
        private readonly IPharmacyDispenseService _pharmacyDispenseService;

        public PharmacyDispenseController(IPharmacyDispenseService pharmacyDispenseService)
        {
            _pharmacyDispenseService = pharmacyDispenseService;
        }

        [HttpGet]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetAllDispenses()
        {
            var dispenses = await _pharmacyDispenseService.GetAllDispensesAsync();
            return Ok(dispenses);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetDispenseById(int id)
        {
            var dispense = await _pharmacyDispenseService.GetDispenseByIdAsync(id);
            if (dispense == null) return NotFound();
            return Ok(dispense);
        }

        [HttpGet("prescription/{prescriptionId}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetDispensesByPrescription(int prescriptionId)
        {
            var dispenses = await _pharmacyDispenseService.GetDispensesByPrescriptionAsync(prescriptionId);
            return Ok(dispenses);
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetDispensesByPatient(int patientId)
        {
            var dispenses = await _pharmacyDispenseService.GetDispensesByPatientAsync(patientId);
            return Ok(dispenses);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor,Nurse")]
        public async Task<IActionResult> CreateDispense([FromBody] PharmacyDispenseCreateDto dto)
        {
            var dispensedById = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(dispensedById))
                return Unauthorized();

            var result = await _pharmacyDispenseService.CreateDispenseAsync(dto, dispensedById);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Nurse")]
        public async Task<IActionResult> UpdateDispense(int id, [FromBody] PharmacyDispenseUpdateDto dto)
        {
            var result = await _pharmacyDispenseService.UpdateDispenseAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Doctor,Nurse")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            var success = await _pharmacyDispenseService.UpdateStatusAsync(id, newStatus);
            if (!success) return BadRequest("Invalid status transition.");
            return Ok();
        }
    }
}
