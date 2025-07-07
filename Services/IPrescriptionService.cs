using Sanalink.API.DTOs;

namespace Sanalink.API.Services;

public interface IPrescriptionService
{
    Task<IEnumerable<PrescriptionReadDto>> GetPrescriptionsForPatientAsync(int patientId);
    Task<PrescriptionReadDto> CreatePrescriptionAsync(PrescriptionCreateDto dto, string doctorId);
}

