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
    public class EncounterController : ControllerBase
    {
        private readonly IEncounterService _encounterService;
        private readonly AppDbContext _db;

        public EncounterController(IEncounterService encounterService, AppDbContext db)
        {
            _encounterService = encounterService;
            _db = db;
        }

        [HttpGet("analytics")]
        [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
        public async Task<IActionResult> GetEncounterAnalytics()
        {
            var encounters = await _encounterService.GetAllEncountersAsync();
            var list = encounters.ToList();
            return Ok(new
            {
                total = list.Count,
                open = list.Count(e => e.Status == "Open"),
                inProgress = list.Count(e => e.Status == "InProgress"),
                closed = list.Count(e => e.Status == "Closed"),
            });
        }

        [HttpGet]
        [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
        public async Task<IActionResult> GetAllEncounters([FromQuery] string? status = null)
        {
            var encounters = await _encounterService.GetAllEncountersAsync(status);
            return Ok(encounters);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
        public async Task<IActionResult> GetEncounterById(int id)
        {
            var encounter = await _encounterService.GetEncounterByIdAsync(id);
            if (encounter == null) return NotFound();
            return Ok(encounter);
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
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
        [Authorize(Roles = "Doctor,Nurse")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] StatusUpdateDto dto)
        {
            var success = await _encounterService.UpdateStatusAsync(id, dto.Status);
            if (!success) return BadRequest("Invalid status transition.");
            return Ok();
        }

        [HttpPost("seed-analytics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SeedAnalytics()
        {
            var diagnoses = new[]
            {
                "Paludisme", "Hypertension", "Diabète", "Tuberculose", "Asthme",
                "Grippe", "VIH/SIDA", "Diarrhée aiguë", "Anémie", "Infection urinaire"
            };

            var encounters = await _db.Encounters.ToListAsync();
            for (int i = 0; i < encounters.Count; i++)
            {
                encounters[i].Diagnosis = diagnoses[i % diagnoses.Length];
                encounters[i].CreatedAt = DateTime.UtcNow.AddDays(-(i % 90));
            }

            await _db.SaveChangesAsync();
            return Ok(new { updated = encounters.Count });
        }

        [HttpGet("top-diagnoses")]
        [Authorize(Roles = "Admin,DAF")]
        public async Task<IActionResult> GetTopDiagnoses([FromQuery] int limit = 10)
        {
            var data = await _db.Encounters
                .Where(e => e.Diagnosis != null && e.Diagnosis != "")
                .GroupBy(e => e.Diagnosis!)
                .Select(g => new { Diagnosis = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(limit)
                .ToListAsync();

            return Ok(data.Select(d => new { diagnosis = d.Diagnosis, count = d.Count }));
        }

        [HttpGet("chronic-diseases")]
        [Authorize(Roles = "Admin,DAF")]
        public async Task<IActionResult> GetChronicDiseases([FromQuery] int days = 90)
        {
            var since = DateTime.UtcNow.AddDays(-days);

            var keywords = new[]
            {
                ("Diabète", new[] { "diabet", "diabète" }),
                ("Hypertension", new[] { "hypertension", "hta" }),
                ("Paludisme", new[] { "palud", "malaria" }),
                ("Tuberculose", new[] { "tubercul" }),
                ("Asthme", new[] { "asthme", "asthma" }),
                ("VIH/SIDA", new[] { "vih", "sida", "hiv", "aids" }),
            };

            var encounters = await _db.Encounters
                .Where(e => e.Diagnosis != null && e.Diagnosis != "" && e.CreatedAt >= since)
                .Select(e => e.Diagnosis!.ToLower())
                .ToListAsync();

            var result = keywords.Select(kw => new
            {
                disease = kw.Item1,
                count = encounters.Count(d => kw.Item2.Any(k => d.Contains(k)))
            }).ToList();

            return Ok(result);
        }

        [HttpGet("pediatric")]
        [Authorize(Roles = "Admin,DAF")]
        public async Task<IActionResult> GetPediatric([FromQuery] int days = 30)
        {
            var since = DateTime.UtcNow.AddDays(-(days - 1)).Date;
            var fiveYearsAgo = DateTime.UtcNow.AddYears(-5);

            var data = await _db.Encounters
                .Join(_db.Patients, e => e.PatientId, p => p.Id, (e, p) => new { e.CreatedAt, p.DateOfBirth })
                .Where(x => x.CreatedAt >= since && x.DateOfBirth >= fiveYearsAgo)
                .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month, x.CreatedAt.Day })
                .Select(g => new { g.Key.Year, g.Key.Month, g.Key.Day, Count = g.Count() })
                .ToListAsync();

            var allDates = Enumerable.Range(0, days).Select(i => since.AddDays(i)).ToList();
            return Ok(new
            {
                dates = allDates.Select(d => d.ToString("dd/MM")),
                counts = allDates.Select(d => data.FirstOrDefault(x => x.Year == d.Year && x.Month == d.Month && x.Day == d.Day)?.Count ?? 0)
            });
        }

        [HttpGet("per-facility")]
        [Authorize(Roles = "Admin,DAF")]
        public async Task<IActionResult> GetEncounterPerFacility()
        {
            var data = await _db.Encounters
                .Join(_db.Patients, e => e.PatientId, p => p.Id, (e, p) => new { p.FacilityId })
                .GroupBy(x => x.FacilityId)
                .Select(g => new { FacilityId = g.Key, Count = g.Count() })
                .ToListAsync();

            var facilities = await _db.Facilities.ToListAsync();

            var result = data.Select(d => new
            {
                facilityId = d.FacilityId,
                facilityName = facilities.FirstOrDefault(f => f.Id == d.FacilityId)?.Name ?? $"Établissement {d.FacilityId}",
                encounterCount = d.Count
            }).ToList();

            return Ok(result);
        }
    }
}
