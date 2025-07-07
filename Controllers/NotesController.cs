using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sanalink.API.DTOs;
using Sanalink.API.Services;
using System.Security.Claims;

namespace Sanalink.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotesController : ControllerBase
    {
        private readonly INoteService _noteService;

        public NotesController(INoteService noteService)
        {
            _noteService = noteService;
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetNotesForPatient(int patientId)
        {
            var notes = await _noteService.GetNotesForPatientAsync(patientId);
            return Ok(notes);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> CreateNote([FromBody] NoteCreateDto dto)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(doctorId))
                return Unauthorized();

            var result = await _noteService.CreateNoteAsync(dto, doctorId);
            return Ok(result);
        }
    }
}
