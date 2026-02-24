using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Models;

namespace Sanalink.API.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly AppDbContext _context;

        public PrescriptionService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PrescriptionReadDto>> GetAllPrescriptionsAsync()
        {
            var prescriptions = await _context.Prescriptions
                .Include(p => p.Patient)
                .Include(p => p.Doctor)
                .ToListAsync();

            return prescriptions.Select(p => new PrescriptionReadDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                PatientName = p.Patient.FirstName + " " + p.Patient.LastName,
                DoctorName = p.Doctor.UserName!,
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                CreatedAt = p.CreatedAt
            });
        }

        public async Task<IEnumerable<PrescriptionReadDto>> GetPrescriptionsForPatientAsync(int patientId)
        {
            return await _context.Prescriptions
                .Where(p => p.PatientId == patientId)
                .Include(p => p.Doctor)
                .Select(p => new PrescriptionReadDto
                {
                    Id = p.Id,
                    MedicationName = p.MedicationName,
                    Dosage = p.Dosage,
                    Instructions = p.Instructions,
                    DoctorName = p.Doctor.FullName,
                    CreatedAt = p.CreatedAt
                }).ToListAsync();
        }

        public async Task<PrescriptionReadDto> CreatePrescriptionAsync(PrescriptionCreateDto dto, string doctorId)
        {
            var prescription = new Prescription
            {
                PatientId = dto.PatientId,
                MedicationName = dto.MedicationName,
                Dosage = dto.Dosage,
                Instructions = dto.Instructions,
                DoctorId = doctorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Prescriptions.Add(prescription);
            await _context.SaveChangesAsync();

            return new PrescriptionReadDto
            {
                Id = prescription.Id,
                MedicationName = prescription.MedicationName,
                Dosage = prescription.Dosage,
                Instructions = prescription.Instructions,
                DoctorName = (await _context.Users.FindAsync(doctorId))?.FullName ?? "Unknown",
                CreatedAt = prescription.CreatedAt
            };
        }
    }
}
