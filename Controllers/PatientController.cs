using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.Models;
using System.Security.Claims;

namespace Sanalink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    private readonly AppDbContext _db;

    public PatientController(AppDbContext db)
    {
        _db = db;
    }

    // GET: /api/patient
    [HttpGet]
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    public async Task<IActionResult> GetPatients()
    {
        var patients = await _db.Patients
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return Ok(patients);
    }

    [HttpGet("registrations/last7days")]
    [Authorize(Roles = "Admin,Doctor,Nurse")]
    public async Task<IActionResult> GetRegistrationsLast7Days()
    {
        var endDate = DateTime.UtcNow.Date;
        var startDate = endDate.AddDays(-6);

        var registrations = await _db.Patients
            .Where(p => p.CreatedAt.Date >= startDate && p.CreatedAt.Date <= endDate)
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        // Fill missing days with 0
        var result = Enumerable.Range(0, 7)
            .Select(i => startDate.AddDays(i))
            .Select(date => new
            {
                Date = date.ToString("yyyy-MM-dd"),
                Count = registrations.FirstOrDefault(r => r.Date == date)?.Count ?? 0
            });

        return Ok(result);
    }


    // POST: /api/patient
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> CreatePatient(Patient patient)
    {
        patient.CreatedAt = DateTime.Now;
        patient.CreatedBy = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPatientById), new { id = patient.Id }, patient);
    }

    // GET: /api/patient/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Doctor, Nurse, Admin")]
    public async Task<IActionResult> GetPatientById(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient == null) return NotFound();

        return Ok(patient);
    }

    // PUT: /api/patient/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> UpdatePatient(int id, Patient updated)
    {
        var existing = await _db.Patients.FindAsync(id);
        if (existing == null) return NotFound();

        existing.FirstName = updated.FirstName;
        existing.LastName = updated.LastName;
        existing.DateOfBirth = updated.DateOfBirth;
        existing.Gender = updated.Gender;

        await _db.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("recent")]
    [Authorize(Roles = "Admin, Doctor, Nurse")]
    public async Task<IActionResult> GetRecentRegistration()
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        var recentCount = await _db.Patients.CountAsync(p => p.CreatedAt >= cutoff);

        return Ok(recentCount);
    }
}