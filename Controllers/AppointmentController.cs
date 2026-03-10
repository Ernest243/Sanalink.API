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
public class AppointmentController : ControllerBase
{
    private readonly AppDbContext _db;

    public AppointmentController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<IActionResult> GetAppointment()
    {
        var appointments = await _db.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .OrderByDescending(a => a.Date)
            .Select(a => new AppointmentReadDto
            {
                Id = a.Id,
                PatientId = a.PatientId,
                PatientName = a.Patient != null
                    ? (a.Patient.FirstName + " " + a.Patient.LastName).Trim()
                    : "",
                DoctorId = a.DoctorId,
                DoctorName = a.Doctor != null
                    ? (a.Doctor.FirstName + " " + a.Doctor.LastName).Trim()
                    : "",
                Date = a.Date,
                Reason = a.Reason,
                Status = a.Status,
                CreatedAt = a.CreateAt
            })
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<IActionResult> GetById(int id)
    {
        var a = await _db.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (a is null) return NotFound();

        return Ok(new AppointmentReadDto
        {
            Id = a.Id,
            PatientId = a.PatientId,
            PatientName = a.Patient != null
                ? (a.Patient.FirstName + " " + a.Patient.LastName).Trim()
                : "",
            DoctorId = a.DoctorId,
            DoctorName = a.Doctor != null
                ? (a.Doctor.FirstName + " " + a.Doctor.LastName).Trim()
                : "",
            Date = a.Date,
            Reason = a.Reason,
            Status = a.Status,
            CreatedAt = a.CreateAt
        });
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
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

        // Return DTO with resolved names
        var doctor = await _db.Users.FindAsync(appointment.DoctorId);
        var patient = await _db.Patients.FindAsync(appointment.PatientId);

        return Ok(new AppointmentReadDto
        {
            Id = appointment.Id,
            PatientId = appointment.PatientId,
            PatientName = patient != null
                ? (patient.FirstName + " " + patient.LastName).Trim()
                : "",
            DoctorId = appointment.DoctorId,
            DoctorName = doctor != null
                ? (doctor.FirstName + " " + doctor.LastName).Trim()
                : "",
            Date = appointment.Date,
            Reason = appointment.Reason,
            Status = appointment.Status,
            CreatedAt = appointment.CreateAt
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Doctor,Accueil,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] AppointmentUpdateDto dto)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt is null) return NotFound();

        appt.Date = dto.Date;
        appt.Reason = dto.Reason;
        if (!string.IsNullOrWhiteSpace(dto.Status))
            appt.Status = dto.Status;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Doctor,Admin,Accueil")]
    public async Task<IActionResult> Cancel(int id)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt is null) return NotFound();

        appt.Status = "Cancelled";
        await _db.SaveChangesAsync();

        return Ok(new { message = "Appointment cancelled" });
    }

    [HttpGet("appointments-per-day")]
    [Authorize(Roles = "Doctor,Admin,Nurse,Accueil")]
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
    [Authorize(Roles = "Doctor,Admin,Nurse,Accueil")]
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
