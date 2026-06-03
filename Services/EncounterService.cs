using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Infrastructure;
using Sanalink.API.Models;

namespace Sanalink.API.Services
{
    public class EncounterService : IEncounterService
    {
        private readonly AppDbContext _context;
        private readonly IFeatureManager _features;

        public EncounterService(AppDbContext context, IFeatureManager features)
        {
            _context = context;
            _features = features;
        }

        public async Task<IEnumerable<EncounterReadDto>> GetAllEncountersAsync(string? status = null)
        {
            var icd10 = await _features.IsEnabledAsync(FeatureFlags.ICD10);

            var query = _context.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .AsQueryable();

            if (icd10)
                query = query.Include(e => e.Diagnoses);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);

            var encounters = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
            return encounters.Select(e => MapToReadDto(e, icd10));
        }

        public async Task<EncounterReadDto?> GetEncounterByIdAsync(int id)
        {
            var icd10 = await _features.IsEnabledAsync(FeatureFlags.ICD10);

            var query = _context.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .AsQueryable();

            if (icd10)
                query = query.Include(e => e.Diagnoses);

            var encounter = await query.FirstOrDefaultAsync(e => e.Id == id);
            return encounter is null ? null : MapToReadDto(encounter, icd10);
        }

        public async Task<IEnumerable<EncounterReadDto>> GetEncountersByPatientAsync(int patientId)
        {
            var icd10 = await _features.IsEnabledAsync(FeatureFlags.ICD10);

            var query = _context.Encounters
                .Where(e => e.PatientId == patientId)
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .AsQueryable();

            if (icd10)
                query = query.Include(e => e.Diagnoses);

            var encounters = await query.OrderByDescending(e => e.CreatedAt).ToListAsync();
            return encounters.Select(e => MapToReadDto(e, icd10));
        }

        public async Task<EncounterReadDto> CreateEncounterAsync(EncounterCreateDto dto, string doctorId)
        {
            var icd10 = await _features.IsEnabledAsync(FeatureFlags.ICD10);
            var encounterNumber = await GenerateEncounterNumberAsync();

            var encounter = new Encounter
            {
                EncounterNumber = encounterNumber,
                PatientId       = dto.PatientId,
                DoctorId        = doctorId,
                Status          = "Open",
                ChiefComplaint  = dto.ChiefComplaint,
                Vitals          = dto.Vitals,
                CreatedAt       = DateTime.UtcNow
            };

            _context.Encounters.Add(encounter);
            await _context.SaveChangesAsync();

            if (icd10 && dto.Diagnoses?.Any() == true)
            {
                var diagnoses = dto.Diagnoses.Select(d => new EncounterDiagnosis
                {
                    EncounterId      = encounter.Id,
                    ICD10Code        = d.ICD10Code,
                    ICD10Description = d.ICD10Description,
                    DiagnosisType    = d.DiagnosisType,
                    Rank             = d.Rank
                });
                _context.EncounterDiagnoses.AddRange(diagnoses);
                await _context.SaveChangesAsync();
            }

            var created = await _context.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .Include(e => e.Diagnoses)
                .FirstAsync(e => e.Id == encounter.Id);

            return MapToReadDto(created, icd10);
        }

        public async Task<EncounterReadDto?> UpdateEncounterAsync(int id, EncounterUpdateDto dto)
        {
            var icd10 = await _features.IsEnabledAsync(FeatureFlags.ICD10);

            var encounter = await _context.Encounters
                .Include(e => e.Patient)
                .Include(e => e.Doctor)
                .Include(e => e.Nurse)
                .Include(e => e.Diagnoses)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (encounter is null) return null;

            if (dto.ChiefComplaint != null) encounter.ChiefComplaint = dto.ChiefComplaint;
            if (dto.Vitals        != null) encounter.Vitals          = dto.Vitals;
            if (dto.Diagnosis     != null) encounter.Diagnosis        = dto.Diagnosis;
            if (dto.ClinicalNotes != null) encounter.ClinicalNotes    = dto.ClinicalNotes;
            if (dto.NurseId       != null) encounter.NurseId          = dto.NurseId;

            encounter.UpdatedAt = DateTime.UtcNow;

            // ICD-10: replace all diagnoses when list is provided
            if (icd10 && dto.Diagnoses != null)
            {
                if (encounter.Diagnoses?.Any() == true)
                    _context.EncounterDiagnoses.RemoveRange(encounter.Diagnoses);

                _context.EncounterDiagnoses.AddRange(dto.Diagnoses.Select(d => new EncounterDiagnosis
                {
                    EncounterId      = encounter.Id,
                    ICD10Code        = d.ICD10Code,
                    ICD10Description = d.ICD10Description,
                    DiagnosisType    = d.DiagnosisType,
                    Rank             = d.Rank
                }));
            }

            await _context.SaveChangesAsync();

            if (dto.NurseId != null)
                await _context.Entry(encounter).Reference(e => e.Nurse).LoadAsync();

            if (icd10)
                await _context.Entry(encounter).Collection(e => e.Diagnoses!).LoadAsync();

            return MapToReadDto(encounter, icd10);
        }

        public async Task<bool> UpdateStatusAsync(int id, string newStatus)
        {
            var encounter = await _context.Encounters.FindAsync(id);
            if (encounter is null) return false;

            var validTransitions = new Dictionary<string, string>
            {
                { "Open",       "InProgress" },
                { "InProgress", "Closed"     }
            };

            if (!validTransitions.TryGetValue(encounter.Status, out var expectedNext) || expectedNext != newStatus)
                return false;

            encounter.Status    = newStatus;
            encounter.UpdatedAt = DateTime.UtcNow;

            if (newStatus == "Closed")
                encounter.ClosedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<string> GenerateEncounterNumberAsync()
        {
            var today  = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"ENC-{today}-";

            var lastEncounter = await _context.Encounters
                .Where(e => e.EncounterNumber.StartsWith(prefix))
                .OrderByDescending(e => e.EncounterNumber)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (lastEncounter != null)
            {
                var lastNumberStr = lastEncounter.EncounterNumber[prefix.Length..];
                if (int.TryParse(lastNumberStr, out int lastNumber))
                    nextNumber = lastNumber + 1;
            }

            return $"{prefix}{nextNumber:D3}";
        }

        private static EncounterReadDto MapToReadDto(Encounter e, bool includeDiagnoses)
        {
            var dto = new EncounterReadDto
            {
                Id              = e.Id,
                EncounterNumber = e.EncounterNumber,
                PatientId       = e.PatientId,
                PatientName     = e.Patient.FirstName + " " + e.Patient.LastName,
                DoctorName      = e.Doctor.FullName ?? e.Doctor.UserName!,
                NurseName       = e.Nurse?.FullName ?? e.Nurse?.UserName,
                Status          = e.Status,
                ChiefComplaint  = e.ChiefComplaint,
                Vitals          = e.Vitals,
                Diagnosis       = e.Diagnosis,
                ClinicalNotes   = e.ClinicalNotes,
                CreatedAt       = e.CreatedAt,
                UpdatedAt       = e.UpdatedAt,
                ClosedAt        = e.ClosedAt
            };

            if (includeDiagnoses && e.Diagnoses?.Any() == true)
            {
                dto.Diagnoses = e.Diagnoses
                    .OrderBy(d => d.Rank)
                    .Select(d => new EncounterDiagnosisReadDto
                    {
                        Id               = d.Id,
                        ICD10Code        = d.ICD10Code,
                        ICD10Description = d.ICD10Description,
                        DiagnosisType    = d.DiagnosisType,
                        Rank             = d.Rank
                    }).ToList();
            }

            return dto;
        }
    }
}
