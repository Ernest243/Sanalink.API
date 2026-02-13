using Sanalink.API.DTOs;

namespace Sanalink.API.Services
{
    public interface IEncounterService
    {
        Task<IEnumerable<EncounterReadDto>> GetAllEncountersAsync();
        Task<EncounterReadDto?> GetEncounterByIdAsync(int id);
        Task<IEnumerable<EncounterReadDto>> GetEncountersByPatientAsync(int patientId);
        Task<EncounterReadDto> CreateEncounterAsync(EncounterCreateDto dto, string doctorId);
        Task<EncounterReadDto?> UpdateEncounterAsync(int id, EncounterUpdateDto dto);
        Task<bool> UpdateStatusAsync(int id, string newStatus);
    }
}
