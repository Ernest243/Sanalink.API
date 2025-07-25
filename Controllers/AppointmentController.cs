using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.Dtos;
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
    [Authorize(Roles = "Doctor, Nurse, Admin")]
    public async Task<IActionResult> BookAppointment([FromBody] AppointmentCreateDto dto)
    {
        var appointment = new Appointment
        {
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            Date = dto.Date,
            Reason = dto.Reason,
            Status = "Scheduled",
            CreateAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();

        return Ok(appointment);
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

    [HttpGet("appointments-per-day")]
    [Authorize(Roles = "Doctor,Admin, Nurse")]
    public async Task<IActionResult> GetAppointmentsPerDay()
    {
        var now = DateTime.UtcNow;
        var start = now.AddDays(-9).Date;

        var data = await _db.Appointments
            .Where(a => a.Date >= start)
            .GroupBy(a => a.Date.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var result = Enumerable.Range(0, 10).Select(i => start.AddDays(i)).ToList();

        var response = new
        {
            dates = result.Select(d => d.ToString("MM-dd")),
            counts = result.Select(d => data.FirstOrDefault(x => x.Date == d)?.Count ?? 0)
        };

        return Ok(response);
    }

    [HttpGet("analytics")]
    [Authorize(Roles = "Doctor,Admin, Nurse")]
    public async Task<IActionResult> GetAppointmentAnalytics()
    {
        var appointments = await _db.Appointments.ToListAsync();
        var total = appointments.Count;
        var scheduled = appointments.Count(a => a.Status == "Scheduled");
        var completed = appointments.Count(a => a.Status == "Completed");
        var cancelled = appointments.Count(a => a.Status == "Cancelled");

        var totalPatients = await _db.Patients.CountAsync();
        var totalPrescriptions = await _db.Prescriptions.CountAsync();

        return Ok(new
        {
            totalAppointments = total,
            scheduled,
            completed,
            cancelled,
            totalPatients,
            totalPrescriptions
        });
    }

}