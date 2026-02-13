using Microsoft.EntityFrameworkCore;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Models;

namespace Sanalink.API.Services
{
    public class FacilityService : IFacilityService
    {
        private readonly AppDbContext _context;

        public FacilityService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FacilityReadDto>> GetAllFacilitiesAsync()
        {
            return await _context.Facilities
                .OrderBy(f => f.Name)
                .Select(f => new FacilityReadDto
                {
                    Id = f.Id,
                    Name = f.Name,
                    Address = f.Address,
                    Phone = f.Phone,
                    Email = f.Email,
                    IsActive = f.IsActive,
                    CreatedAt = f.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<FacilityReadDto?> GetFacilityByIdAsync(int id)
        {
            var facility = await _context.Facilities.FindAsync(id);
            if (facility == null) return null;

            return new FacilityReadDto
            {
                Id = facility.Id,
                Name = facility.Name,
                Address = facility.Address,
                Phone = facility.Phone,
                Email = facility.Email,
                IsActive = facility.IsActive,
                CreatedAt = facility.CreatedAt
            };
        }

        public async Task<FacilityReadDto> CreateFacilityAsync(FacilityCreateDto dto)
        {
            var facility = new Facility
            {
                Name = dto.Name,
                Address = dto.Address,
                Phone = dto.Phone,
                Email = dto.Email,
                CreatedAt = DateTime.UtcNow
            };

            _context.Facilities.Add(facility);
            await _context.SaveChangesAsync();

            return new FacilityReadDto
            {
                Id = facility.Id,
                Name = facility.Name,
                Address = facility.Address,
                Phone = facility.Phone,
                Email = facility.Email,
                IsActive = facility.IsActive,
                CreatedAt = facility.CreatedAt
            };
        }
    }
}
