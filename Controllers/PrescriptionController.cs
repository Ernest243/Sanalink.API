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
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _service;

        public PrescriptionsController(IPrescriptionService service)
        {
            _service = service;
        }

        [HttpGet("my")]
        [Authorize(Roles = "Patient")]
        public async Task<IActionResult> GetMyPrescriptions()
        {
            var patientIdClaim = User.FindFirstValue("patientId");
            if (!int.TryParse(patientIdClaim, out int patientId))
                return Unauthorized();

            var prescriptions = await _service.GetPrescriptionsForPatientAsync(patientId);
            return Ok(prescriptions);
        }

        [HttpGet]
        [Authorize(Roles = "Doctor, Nurse, Admin")]
        public async Task<IActionResult> GetAllPrescriptions()
        {
            var prescriptions = await _service.GetAllPrescriptionsAsync();
            return Ok(prescriptions);
        }

        [HttpGet("patient/{patientId}")]
        public async Task<IActionResult> GetPrescriptionsForPatient(int patientId)
        {
            var prescriptions = await _service.GetPrescriptionsForPatientAsync(patientId);
            return Ok(prescriptions);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> CreatePrescription([FromBody] PrescriptionCreateDto dto)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(doctorId))
                return Unauthorized();

            var result = await _service.CreatePrescriptionAsync(dto, doctorId);
            return Ok(result);
        }
    }
}
