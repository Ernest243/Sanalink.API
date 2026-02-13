using Sanalink.API.DTOs;

namespace Sanalink.API.Services
{
    public interface ILabOrderService
    {
        Task<IEnumerable<LabOrderReadDto>> GetAllLabOrdersAsync();
        Task<LabOrderReadDto?> GetLabOrderByIdAsync(int id);
        Task<IEnumerable<LabOrderReadDto>> GetLabOrdersByEncounterAsync(int encounterId);
        Task<IEnumerable<LabOrderReadDto>> GetLabOrdersByPatientAsync(int patientId);
        Task<LabOrderReadDto> CreateLabOrderAsync(LabOrderCreateDto dto, string doctorId);
        Task<LabOrderReadDto?> UpdateLabOrderAsync(int id, LabOrderUpdateDto dto);
        Task<bool> UpdateStatusAsync(int id, string newStatus);
    }
}
