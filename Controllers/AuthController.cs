using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;
using Microsoft.IdentityModel.Tokens;
using Sanalink.API.Data;
using Sanalink.API.DTOs;
using Sanalink.API.Infrastructure;
using Sanalink.API.Models;
using Sanalink.API.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Sanalink.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IFeatureManager _features;
    private readonly ITokenRevocationService _tokenRevocation;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration config,
        AppDbContext db,
        IFeatureManager features,
        ITokenRevocationService tokenRevocation)
    {
        _userManager     = userManager;
        _signInManager   = signInManager;
        _config          = config;
        _db              = db;
        _features        = features;
        _tokenRevocation = tokenRevocation;
    }

    [HttpPost("register-staff")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> RegisterStaff(StaffRegisterDto dto)
    {
        var allowedRoles = new[] { "Doctor", "Nurse", "Admin", "Accueil", "Caisse", "DAF", "LabTech", "Pharmacist" };
        if (!allowedRoles.Contains(dto.Role))
            return BadRequest($"Role must be one of: {string.Join(", ", allowedRoles)}");

        var existingUser = await _userManager.FindByNameAsync(dto.Email);
        if (existingUser != null)
            return BadRequest("User already exists");

        var user = new ApplicationUser
        {
            UserName       = dto.Email,
            Email          = dto.Email,
            FirstName      = dto.FirstName,
            LastName       = dto.LastName,
            Role           = dto.Role,
            Department     = dto.Department,
            FacilityId     = dto.FacilityId,
            EmailConfirmed = true,
            IsActive       = true
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
            return BadRequest(result.Errors);

        await _userManager.AddToRoleAsync(user, dto.Role);
        return Ok("Registration successful");
    }

    [HttpGet("staff")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllStaff()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new StaffReadDto
            {
                Id         = u.Id,
                Email      = u.Email ?? "",
                FirstName  = u.FirstName ?? "",
                LastName   = u.LastName ?? "",
                Role       = u.Role,
                Department = u.Department,
                FacilityId = u.FacilityId,
                IsActive   = u.IsActive,
                CreatedAt  = u.CreateAt
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpPost("login")]
    [EnableRateLimiting(FeatureFlags.RatePolicy_AuthLogin)]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var lockoutEnabled = await _features.IsEnabledAsync(FeatureFlags.ISO27001_AccountLockout);

        var user = await _userManager.FindByNameAsync(dto.Email);
        if (user == null || !user.IsActive)
            return Unauthorized("Invalid credentials");

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: lockoutEnabled);

        if (result.IsLockedOut)
            return StatusCode(429, new { error = "Account is temporarily locked after too many failed attempts. Try again in 15 minutes." });

        if (!result.Succeeded)
            return Unauthorized("Invalid credentials");

        var token = GenerateJwtToken(user);
        return Ok(new { token });
    }

    /// <summary>
    /// Revokes the caller's current token so it cannot be reused.
    /// Effective only when ISO27001_TokenRevocation is enabled.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        if (await _features.IsEnabledAsync(FeatureFlags.ISO27001_TokenRevocation))
        {
            var jti    = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var expStr = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

            DateTime expiresAt = DateTime.UtcNow.AddHours(4);
            if (long.TryParse(expStr, out long expUnix))
                expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

            if (!string.IsNullOrEmpty(jti))
                await _tokenRevocation.RevokeAsync(jti, userId!, expiresAt);
        }

        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("doctors")]
    [Authorize]
    public async Task<IActionResult> GetDoctors()
    {
        var facilityIdClaim = User.FindFirstValue("facilityId");
        int.TryParse(facilityIdClaim, out int facilityId);

        var doctors = await _userManager.Users
            .Where(u => u.Role == "Doctor" && u.IsActive)
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Department, u.FacilityId })
            .ToListAsync();

        if (facilityId > 0)
            doctors = doctors.Where(d => d.FacilityId == facilityId).ToList();

        return Ok(doctors.Select(d => new
        {
            d.Id,
            d.FirstName,
            d.LastName,
            d.Department,
            fullName = (d.FirstName + " " + d.LastName).Trim()
        }));
    }

    [HttpGet("active-staff-count")]
    [Authorize(Roles = "Admin,Doctor,Nurse,DAF")]
    public async Task<IActionResult> GetActiveStaffCount()
    {
        var facilityIdClaim = User.FindFirstValue("facilityId");
        int.TryParse(facilityIdClaim, out int facilityId);

        var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
        var nurses  = await _userManager.GetUsersInRoleAsync("Nurse");

        var doctorCount = facilityId > 0 ? doctors.Count(d => d.FacilityId == facilityId) : doctors.Count;
        var nurseCount  = facilityId > 0 ? nurses.Count(n => n.FacilityId == facilityId)  : nurses.Count;

        return Ok(new { doctors = doctorCount, nurseCount });
    }

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub,   user.Id),
            new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),  // required for token revocation
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim("firstName",          user.FirstName ?? ""),
            new Claim("lastName",           user.LastName  ?? ""),
            new Claim(ClaimTypes.Role,      user.Role ?? ""),
            new Claim("role",               user.Role ?? ""),
            new Claim("facilityId",         user.FacilityId?.ToString() ?? "")
        };

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            claims:            claims,
            expires:           DateTime.UtcNow.AddHours(4),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
