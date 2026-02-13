using Sanalink.API.DTOs;

namespace Sanalink.API.Services
{
    public interface IFacilityService
    {
        Task<IEnumerable<FacilityReadDto>> GetAllFacilitiesAsync();
        Task<FacilityReadDto?> GetFacilityByIdAsync(int id);
        Task<FacilityReadDto> CreateFacilityAsync(FacilityCreateDto dto);
    }
}
