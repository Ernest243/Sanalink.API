using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.Dtos;
using Sanalink.API.Models;
using System.Security.Claims;

namespace Sanalink.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PatientController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<IActionResult> GetPatients()
    {
        var facilityIdClaim = User.FindFirstValue("facilityId");
        int.TryParse(facilityIdClaim, out int facilityId);

        var query = _db.Patients.AsQueryable();

        if (facilityId > 0)
            query = query.Where(p => p.FacilityId == facilityId);

        var patients = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PatientReadDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                MiddleName = p.MiddleName,
                LastName = p.LastName,
                DateOfBirth = p.DateOfBirth,
                Gender = p.Gender,
                Phone = p.Phone,
                Email = p.Email,
                FacilityId = p.FacilityId,
                CreatedAt = p.CreatedAt,
                CreatedBy = p.CreatedBy
            })
            .ToListAsync();

        return Ok(patients);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<IActionResult> GetPatientById(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is null) return NotFound();

        return Ok(new PatientReadDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            MiddleName = patient.MiddleName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Email = patient.Email,
            FacilityId = patient.FacilityId,
            CreatedAt = patient.CreatedAt,
            CreatedBy = patient.CreatedBy
        });
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<IActionResult> CreatePatient([FromBody] PatientCreateDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var patient = new Patient
        {
            FirstName = dto.FirstName,
            MiddleName = dto.MiddleName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Phone = dto.Phone,
            Email = dto.Email,
            FacilityId = dto.FacilityId,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, new PatientReadDto
        {
            Id = patient.Id,
            FirstName = patient.FirstName,
            MiddleName = patient.MiddleName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Phone = patient.Phone,
            Email = patient.Email,
            FacilityId = patient.FacilityId,
            CreatedAt = patient.CreatedAt,
            CreatedBy = patient.CreatedBy
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<IActionResult> UpdatePatient(int id, [FromBody] PatientUpdateDto dto)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is null) return NotFound();

        if (dto.FirstName != null) patient.FirstName = dto.FirstName;
        if (dto.MiddleName != null) patient.MiddleName = dto.MiddleName;
        if (dto.LastName != null) patient.LastName = dto.LastName;
        if (dto.DateOfBirth.HasValue) patient.DateOfBirth = dto.DateOfBirth.Value;
        if (dto.Gender != null) patient.Gender = dto.Gender;
        if (dto.Phone != null) patient.Phone = dto.Phone;
        if (dto.Email != null) patient.Email = dto.Email;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("search")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<IActionResult> Search([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query is required.");

        var results = await _db.Patients
            .Where(p => (p.FirstName + " " + p.LastName).Contains(query))
            .Take(10)
            .Select(p => new PatientReadDto
            {
                Id = p.Id,
                FirstName = p.FirstName,
                MiddleName = p.MiddleName,
                LastName = p.LastName,
                Gender = p.Gender,
                Phone = p.Phone,
                Email = p.Email,
                FacilityId = p.FacilityId,
                DateOfBirth = p.DateOfBirth,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Admin,Doctor,Nurse,DAF,Accueil")]
    public async Task<IActionResult> GetRecentRegistration()
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var count = await _db.Patients.CountAsync(p => p.CreatedAt >= since);
        return Ok(count);
    }

    [HttpGet("registrations")]
    [Authorize(Roles = "Admin,Doctor,Nurse,DAF,Accueil")]
    public async Task<IActionResult> GetRegistrations([FromQuery] int days = 7)
    {
        var since = DateTime.UtcNow.AddDays(-(days - 1)).Date;

        var data = await _db.Patients
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
}
