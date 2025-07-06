using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.Models;
using System.Security.Claims;

namespace Sanalink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentController : ControllerBase
{
    private readonly AppDbContext _db;

    public AppointmentController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Doctor, Nurse, Admin")]
    public async Task<IActionResult> GetAppointment()
    {
        var appointments = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Doctor, Nurse, Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        var appt = await _db.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.Id == id);

        return appt is null ? NotFound() : Ok(appt);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Create(Appointment appt)
    {
        appt.CreateAt = DateTime.UtcNow;
        appt.DoctorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        _db.Appointments.Add(appt);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = appt.Id }, appt);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Update(int id, Appointment update)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt is null) return NotFound();

        appt.Date = update.Date;
        appt.Reason = update.Reason;
        appt.Status = update.Status;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Cancel(int id)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt is null) return NotFound();

        appt.Status = "Cancelled";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Appointment cancelled" });
    }
}