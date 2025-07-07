using Sanalink.API.DTOs;

namespace Sanalink.API.Services
{
    public interface INoteService
    {
        Task<IEnumerable<NoteReadDto>> GetNotesForPatientAsync(int patientId);
        Task<NoteReadDto> CreateNoteAsync(NoteCreateDto dto, string doctorId);
    }
}
