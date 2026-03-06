using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Services;
using System.Security.Claims;

namespace Sanalink.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _service;
        private readonly AppDbContext _db;

        public PrescriptionsController(IPrescriptionService service, AppDbContext db)
        {
            _service = service;
            _db = db;
        }

        [HttpGet("analytics")]
        [Authorize(Roles = "Doctor,Nurse,Admin,Pharmacist")]
        public async Task<IActionResult> GetAnalytics([FromQuery] int days = 7)
        {
            var since = DateTime.UtcNow.AddDays(-(days - 1)).Date;

            var data = await _db.Prescriptions
                .Where(p => p.CreatedAt >= since)
                .GroupBy(p => p.CreatedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var allDates = Enumerable.Range(0, days).Select(i => since.AddDays(i)).ToList();
            return Ok(new
            {
                dates = allDates.Select(d => d.ToString("dd/MM")),
                counts = allDates.Select(d => data.FirstOrDefault(x => x.Date == d)?.Count ?? 0)
            });
        }

        [HttpGet]
        [Authorize(Roles = "Doctor,Nurse,Admin,Pharmacist")]
        public async Task<IActionResult> GetAllPrescriptions()
        {
            var prescriptions = await _service.GetAllPrescriptionsAsync();
            return Ok(prescriptions);
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Doctor,Nurse,Admin,Pharmacist")]
        public async Task<IActionResult> GetPrescriptionsForPatient(int patientId)
        {
            var prescriptions = await _service.GetPrescriptionsForPatientAsync(patientId);
            return Ok(prescriptions);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
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
