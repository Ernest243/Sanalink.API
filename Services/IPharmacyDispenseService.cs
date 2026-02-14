using Sanalink.API.DTOs;

namespace Sanalink.API.Services
{
    public interface IPharmacyDispenseService
    {
        Task<IEnumerable<PharmacyDispenseReadDto>> GetAllDispensesAsync();
        Task<PharmacyDispenseReadDto?> GetDispenseByIdAsync(int id);
        Task<IEnumerable<PharmacyDispenseReadDto>> GetDispensesByPrescriptionAsync(int prescriptionId);
        Task<IEnumerable<PharmacyDispenseReadDto>> GetDispensesByPatientAsync(int patientId);
        Task<PharmacyDispenseReadDto> CreateDispenseAsync(PharmacyDispenseCreateDto dto, string dispensedById);
        Task<PharmacyDispenseReadDto?> UpdateDispenseAsync(int id, PharmacyDispenseUpdateDto dto);
        Task<bool> UpdateStatusAsync(int id, string newStatus);
    }
}
