using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Models;

namespace Sanalink.API.Services
{
    public class EncounterService : IEncounterService
    {
        private readonly AppDbContext _context;

        public EncounterService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EncounterReadDto>> GetAllEncountersAsync()
        {
            return await _context.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => MapToReadDto(e))
                .ToListAsync();
        }

        public async Task<EncounterReadDto?> GetEncounterByIdAsync(int id)
        {
            var encounter = await _context.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (encounter == null) return null;

            return MapToReadDto(encounter);
        }

        public async Task<IEnumerable<EncounterReadDto>> GetEncountersByPatientAsync(int patientId)
        {
            return await _context.Encounters
                .Where(e => e.PatientId == patientId)
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => MapToReadDto(e))
                .ToListAsync();
        }

        public async Task<EncounterReadDto> CreateEncounterAsync(EncounterCreateDto dto, string doctorId)
        {
            var encounterNumber = await GenerateEncounterNumberAsync();

            var encounter = new Encounter
            {
                EncounterNumber = encounterNumber,
                PatientId = dto.PatientId,
                DoctorId = doctorId,
                Status = "Open",
                ChiefComplaint = dto.ChiefComplaint,
                Vitals = dto.Vitals,
                CreatedAt = DateTime.UtcNow
            };

            _context.Encounters.Add(encounter);
            await _context.SaveChangesAsync();

            // Reload with navigation properties
            var created = await _context.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .FirstAsync(e => e.Id == encounter.Id);

            return MapToReadDto(created);
        }

        public async Task<EncounterReadDto?> UpdateEncounterAsync(int id, EncounterUpdateDto dto)
        {
            var encounter = await _context.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (encounter == null) return null;

            if (dto.ChiefComplaint != null) encounter.ChiefComplaint = dto.ChiefComplaint;
            if (dto.Vitals != null) encounter.Vitals = dto.Vitals;
            if (dto.Diagnosis != null) encounter.Diagnosis = dto.Diagnosis;
            if (dto.ClinicalNotes != null) encounter.ClinicalNotes = dto.ClinicalNotes;
            if (dto.NurseId != null) encounter.NurseId = dto.NurseId;

            encounter.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Reload nurse if changed
            if (dto.NurseId != null)
            {
                await _context.Entry(encounter).Reference(e => e.Nurse).LoadAsync();
            }

            return MapToReadDto(encounter);
        }

        public async Task<bool> UpdateStatusAsync(int id, string newStatus)
        {
            var encounter = await _context.Encounters.FindAsync(id);
            if (encounter == null) return false;

            // Validate status transition: Open -> InProgress -> Closed
            var validTransitions = new Dictionary<string, string>
            {
                { "Open", "InProgress" },
                { "InProgress", "Closed" }
            };

            if (!validTransitions.TryGetValue(encounter.Status, out var expectedNext) || expectedNext != newStatus)
                return false;

            encounter.Status = newStatus;
            encounter.UpdatedAt = DateTime.UtcNow;

            if (newStatus == "Closed")
                encounter.ClosedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> GenerateEncounterNumberAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"ENC-{today}-";

            var lastEncounter = await _context.Encounters
                .Where(e => e.EncounterNumber.StartsWith(prefix))
                .OrderByDescending(e => e.EncounterNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastEncounter != null)
            {
                var lastNumberStr = lastEncounter.EncounterNumber.Substring(prefix.Length);
                if (int.TryParse(lastNumberStr, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return $"{prefix}{nextNumber:D3}";
        }

        private static EncounterReadDto MapToReadDto(Encounter e)
        {
            return new EncounterReadDto
            {
                Id = e.Id,
                EncounterNumber = e.EncounterNumber,
                PatientId = e.PatientId,
                PatientName = e.Patient.FirstName + " " + e.Patient.LastName,
                DoctorName = e.Doctor.FullName ?? e.Doctor.UserName!,
                NurseName = e.Nurse?.FullName ?? e.Nurse?.UserName,
                Status = e.Status,
                ChiefComplaint = e.ChiefComplaint,
                Vitals = e.Vitals,
                Diagnosis = e.Diagnosis,
                ClinicalNotes = e.ClinicalNotes,
                CreatedAt = e.CreatedAt,
                UpdatedAt = e.UpdatedAt,
                ClosedAt = e.ClosedAt
            };
        }
    }
}
