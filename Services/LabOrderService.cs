using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Models;

namespace Sanalink.API.Services
{
    public class LabOrderService : ILabOrderService
    {
        private readonly AppDbContext _context;

        public LabOrderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<LabOrderReadDto>> GetAllLabOrdersAsync()
        {
            return await _context.LabOrders
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .OrderByDescending(l => l.OrderedAt)
                .Select(l => MapToReadDto(l))
                .ToListAsync();
        }

        public async Task<LabOrderReadDto?> GetLabOrderByIdAsync(int id)
        {
            var labOrder = await _context.LabOrders
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (labOrder == null) return null;

            return MapToReadDto(labOrder);
        }

        public async Task<IEnumerable<LabOrderReadDto>> GetLabOrdersByEncounterAsync(int encounterId)
        {
            return await _context.LabOrders
                .Where(l => l.EncounterId == encounterId)
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .OrderByDescending(l => l.OrderedAt)
                .Select(l => MapToReadDto(l))
                .ToListAsync();
        }

        public async Task<IEnumerable<LabOrderReadDto>> GetLabOrdersByPatientAsync(int patientId)
        {
            return await _context.LabOrders
                .Where(l => l.PatientId == patientId)
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .OrderByDescending(l => l.OrderedAt)
                .Select(l => MapToReadDto(l))
                .ToListAsync();
        }

        public async Task<LabOrderReadDto> CreateLabOrderAsync(LabOrderCreateDto dto, string doctorId)
        {
            var labOrder = new LabOrder
            {
                EncounterId = dto.EncounterId,
                PatientId = dto.PatientId,
                DoctorId = doctorId,
                TestName = dto.TestName,
                TestCategory = dto.TestCategory,
                Priority = dto.Priority ?? "Routine",
                ClinicalNotes = dto.ClinicalNotes,
                Status = "Pending",
                OrderedAt = DateTime.UtcNow
            };

            _context.LabOrders.Add(labOrder);
            await _context.SaveChangesAsync();

            var created = await _context.LabOrders
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .FirstAsync(l => l.Id == labOrder.Id);

            return MapToReadDto(created);
        }

        public async Task<LabOrderReadDto?> UpdateLabOrderAsync(int id, LabOrderUpdateDto dto)
        {
            var labOrder = await _context.LabOrders
                .Include(l => l.Patient)
                .Include(l => l.Doctor)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (labOrder == null) return null;

            if (dto.TestName != null) labOrder.TestName = dto.TestName;
            if (dto.TestCategory != null) labOrder.TestCategory = dto.TestCategory;
            if (dto.Priority != null) labOrder.Priority = dto.Priority;
            if (dto.ClinicalNotes != null) labOrder.ClinicalNotes = dto.ClinicalNotes;
            if (dto.Result != null) labOrder.Result = dto.Result;
            if (dto.ResultNotes != null) labOrder.ResultNotes = dto.ResultNotes;

            await _context.SaveChangesAsync();

            return MapToReadDto(labOrder);
        }

        public async Task<bool> UpdateStatusAsync(int id, string newStatus)
        {
            var labOrder = await _context.LabOrders.FindAsync(id);
            if (labOrder == null) return false;

            var validTransitions = new Dictionary<string, string>
            {
                { "Pending", "SampleCollected" },
                { "SampleCollected", "InProgress" },
                { "InProgress", "Completed" },
                { "Completed", "Reviewed" }
            };

            if (!validTransitions.TryGetValue(labOrder.Status, out var expectedNext) || expectedNext != newStatus)
                return false;

            labOrder.Status = newStatus;

            if (newStatus == "Completed")
                labOrder.CompletedAt = DateTime.UtcNow;

            if (newStatus == "Reviewed")
                labOrder.ReviewedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        private static LabOrderReadDto MapToReadDto(LabOrder l)
        {
            return new LabOrderReadDto
            {
                Id = l.Id,
                EncounterId = l.EncounterId,
                PatientId = l.PatientId,
                PatientName = l.Patient.FirstName + " " + l.Patient.LastName,
                DoctorName = l.Doctor.FullName ?? l.Doctor.UserName!,
                TestName = l.TestName,
                TestCategory = l.TestCategory,
                Status = l.Status,
                Priority = l.Priority,
                ClinicalNotes = l.ClinicalNotes,
                Result = l.Result,
                ResultNotes = l.ResultNotes,
                OrderedAt = l.OrderedAt,
                CompletedAt = l.CompletedAt,
                ReviewedAt = l.ReviewedAt
            };
        }
    }
}
