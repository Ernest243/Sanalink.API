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
}