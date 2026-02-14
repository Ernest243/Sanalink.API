using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Models;

namespace Sanalink.API.Services
{
    public class PharmacyDispenseService : IPharmacyDispenseService
    {
        private readonly AppDbContext _context;

        public PharmacyDispenseService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<PharmacyDispenseReadDto>> GetAllDispensesAsync()
        {
            return await _context.PharmacyDispenses
                .Include(d => d.Patient)
                .Include(d => d.DispensedBy)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => MapToReadDto(d))
                .ToListAsync();
        }

        public async Task<PharmacyDispenseReadDto?> GetDispenseByIdAsync(int id)
        {
            var dispense = await _context.PharmacyDispenses
                .Include(d => d.Patient)
                .Include(d => d.DispensedBy)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dispense == null) return null;

            return MapToReadDto(dispense);
        }

        public async Task<IEnumerable<PharmacyDispenseReadDto>> GetDispensesByPrescriptionAsync(int prescriptionId)
        {
            return await _context.PharmacyDispenses
                .Where(d => d.PrescriptionId == prescriptionId)
                .Include(d => d.Patient)
                .Include(d => d.DispensedBy)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => MapToReadDto(d))
                .ToListAsync();
        }

        public async Task<IEnumerable<PharmacyDispenseReadDto>> GetDispensesByPatientAsync(int patientId)
        {
            return await _context.PharmacyDispenses
                .Where(d => d.PatientId == patientId)
                .Include(d => d.Patient)
                .Include(d => d.DispensedBy)
                .OrderByDescending(d => d.CreatedAt)
                .Select(d => MapToReadDto(d))
                .ToListAsync();
        }

        public async Task<PharmacyDispenseReadDto> CreateDispenseAsync(PharmacyDispenseCreateDto dto, string dispensedById)
        {
            var dispense = new PharmacyDispense
            {
                PrescriptionId = dto.PrescriptionId,
                PatientId = dto.PatientId,
                DispensedById = dispensedById,
                MedicationName = dto.MedicationName,
                QuantityDispensed = dto.QuantityDispensed,
                Notes = dto.Notes,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.PharmacyDispenses.Add(dispense);
            await _context.SaveChangesAsync();

            var created = await _context.PharmacyDispenses
                .Include(d => d.Patient)
                .Include(d => d.DispensedBy)
                .FirstAsync(d => d.Id == dispense.Id);

            return MapToReadDto(created);
        }

        public async Task<PharmacyDispenseReadDto?> UpdateDispenseAsync(int id, PharmacyDispenseUpdateDto dto)
        {
            var dispense = await _context.PharmacyDispenses
                .Include(d => d.Patient)
                .Include(d => d.DispensedBy)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dispense == null) return null;

            if (dto.MedicationName != null) dispense.MedicationName = dto.MedicationName;
            if (dto.QuantityDispensed != null) dispense.QuantityDispensed = dto.QuantityDispensed;
            if (dto.Notes != null) dispense.Notes = dto.Notes;

            await _context.SaveChangesAsync();

            return MapToReadDto(dispense);
        }

        public async Task<bool> UpdateStatusAsync(int id, string newStatus)
        {
            var dispense = await _context.PharmacyDispenses.FindAsync(id);
            if (dispense == null) return false;

            var validTransitions = new Dictionary<string, string>
            {
                { "Pending", "Dispensed" },
                { "Dispensed", "Collected" }
            };

            if (!validTransitions.TryGetValue(dispense.Status, out var expectedNext) || expectedNext != newStatus)
                return false;

            dispense.Status = newStatus;

            if (newStatus == "Dispensed")
                dispense.DispensedAt = DateTime.UtcNow;

            if (newStatus == "Collected")
                dispense.CollectedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private static PharmacyDispenseReadDto MapToReadDto(PharmacyDispense d)
        {
            return new PharmacyDispenseReadDto
            {
                Id = d.Id,
                PrescriptionId = d.PrescriptionId,
                PatientId = d.PatientId,
                PatientName = d.Patient.FirstName + " " + d.Patient.LastName,
                DispensedByName = d.DispensedBy.FullName ?? d.DispensedBy.UserName!,
                MedicationName = d.MedicationName,
                QuantityDispensed = d.QuantityDispensed,
                Status = d.Status,
                Notes = d.Notes,
                DispensedAt = d.DispensedAt,
                CollectedAt = d.CollectedAt,
                CreatedAt = d.CreatedAt
            };
        }
    }
}
