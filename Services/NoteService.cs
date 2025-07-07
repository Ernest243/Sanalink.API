using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Models;

namespace Sanalink.API.Services
{
    public class NoteService : INoteService
    {
        private readonly AppDbContext _context;

        public NoteService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NoteReadDto>> GetNotesForPatientAsync(int patientId)
        {
            return await _context.Notes
                .Where(n => n.PatientId == patientId)
                .Include(n => n.Doctor)
                .Select(n => new NoteReadDto
                {
                    Id = n.Id,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt,
                    DoctorName = n.Doctor.FullName!
                }).ToListAsync();
        }

        public async Task<NoteReadDto> CreateNoteAsync(NoteCreateDto dto, string doctorId)
        {
            var note = new Note
            {
                PatientId = dto.PatientId,
                Content = dto.Content,
                DoctorId = doctorId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notes.Add(note);
            await _context.SaveChangesAsync();

            return new NoteReadDto
            {
                Id = note.Id,
                Content = note.Content,
                CreatedAt = note.CreatedAt,
                DoctorName = (await _context.Users.FindAsync(doctorId))?.FullName ?? "Unknown"
            };
        }
    }
}
