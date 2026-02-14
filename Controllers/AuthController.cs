using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Sanalink.API.Data;
using Sanalink.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sanalink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration config,
        AppDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
        _db = db;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(PatientRegisterDto dto)
    {
        var existingUser = await _userManager.FindByNameAsync(dto.Email);
        if (existingUser != null)
            return BadRequest("User already exists");

        var facility = await _db.Facilities.FindAsync(dto.FacilityId);
        if (facility == null)
            return BadRequest("Invalid FacilityId");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = $"{dto.FirstName} {dto.LastName}",
            Role = "Patient",
            FacilityId = dto.FacilityId,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, "Patient");

        var patient = new Patient
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            FacilityId = dto.FacilityId,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = user.Id
        };

        _db.Patients.Add(patient);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var token = GenerateJwtToken(user, patient.Id);
        return Ok(new { token });
    }

    [HttpPost("register-staff")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterStaff(StaffRegisterDto dto)
    {
        var allowedRoles = new[] { "Doctor", "Nurse", "Admin" };
        if (!allowedRoles.Contains(dto.Role))
            return BadRequest("Role must be one of: Doctor, Nurse, Admin");

        var existingUser = await _userManager.FindByNameAsync(dto.Email);
        if (existingUser != null)
            return BadRequest("User already exists");

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName,
            Role = dto.Role,
            Department = dto.Department,
            FacilityId = dto.FacilityId,
            EmailConfirmed = true,
            IsActive = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, dto.Role);

        return Ok("Registration successful");
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.Email);
        if (user == null || !user.IsActive)
            return Unauthorized("Invalid credentials");

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
            return Unauthorized("Invalid credentials");

        int? patientId = null;
        if (user.Role == "Patient")
        {
            var patient = await _db.Patients.FirstOrDefaultAsync(p => p.UserId == user.Id);
            patientId = patient?.Id;
        }

        var token = GenerateJwtToken(user, patientId);
        return Ok(new { token });
    }

    [HttpGet("active-staff-count")]
    [Authorize(Roles = "Admin, Doctor, Nurse")]
    public async Task<IActionResult> GetActiveStaffCount()
    {
        var facilityIdClaim = User.FindFirstValue("facilityId");
        int.TryParse(facilityIdClaim, out int facilityId);

        var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
        var nurses = await _userManager.GetUsersInRoleAsync("Nurse");

        var doctorCount = facilityId > 0
            ? doctors.Count(d => d.FacilityId == facilityId)
            : doctors.Count;

        var nurseCount = facilityId > 0
            ? nurses.Count(n => n.FacilityId == facilityId)
            : nurses.Count;

        return Ok(new
        {
            doctors = doctorCount,
            nurseCount = nurseCount
        });
    }

    private string GenerateJwtToken(ApplicationUser user, int? patientId = null)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim("role", user.Role ?? ""),
            new Claim("facilityId", user.FacilityId?.ToString() ?? "")
        };

        if (patientId.HasValue)
            claims.Add(new Claim("patientId", patientId.Value.ToString()));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(4),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
