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
    public class LabOrderController : ControllerBase
    {
        private readonly ILabOrderService _labOrderService;

        public LabOrderController(ILabOrderService labOrderService)
        {
            _labOrderService = labOrderService;
        }

        [HttpGet]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetAllLabOrders()
        {
            var labOrders = await _labOrderService.GetAllLabOrdersAsync();
            return Ok(labOrders);
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetLabOrderById(int id)
        {
            var labOrder = await _labOrderService.GetLabOrderByIdAsync(id);
            if (labOrder == null) return NotFound();
            return Ok(labOrder);
        }

        [HttpGet("encounter/{encounterId}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetLabOrdersByEncounter(int encounterId)
        {
            var labOrders = await _labOrderService.GetLabOrdersByEncounterAsync(encounterId);
            return Ok(labOrders);
        }

        [HttpGet("patient/{patientId}")]
        [Authorize(Roles = "Doctor,Nurse,Admin")]
        public async Task<IActionResult> GetLabOrdersByPatient(int patientId)
        {
            var labOrders = await _labOrderService.GetLabOrdersByPatientAsync(patientId);
            return Ok(labOrders);
        }

        [HttpPost]
        [Authorize(Roles = "Doctor")]
        public async Task<IActionResult> CreateLabOrder([FromBody] LabOrderCreateDto dto)
        {
            var doctorId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(doctorId))
                return Unauthorized();

            var result = await _labOrderService.CreateLabOrderAsync(dto, doctorId);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Doctor,Nurse")]
        public async Task<IActionResult> UpdateLabOrder(int id, [FromBody] LabOrderUpdateDto dto)
        {
            var result = await _labOrderService.UpdateLabOrderAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }

        [HttpPut("{id}/status")]
        [Authorize(Roles = "Doctor,Nurse")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string newStatus)
        {
            var success = await _labOrderService.UpdateStatusAsync(id, newStatus);
            if (!success) return BadRequest("Invalid status transition.");
            return Ok();
        }
    }
}
