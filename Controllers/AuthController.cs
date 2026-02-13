using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Sanalink.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;

namespace Sanalink.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _config;

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration config)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _config = config;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByNameAsync(dto.Email!);
        if (existingUser != null)
        {
            return BadRequest("User already exists");
        }

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

        var result = await _userManager.CreateAsync(user, dto.Password!);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        // Assign Identity Role (if using IdentityRole)
        if (!await _userManager.IsInRoleAsync(user, dto.Role!))
        {
            await _userManager.AddToRoleAsync(user, dto.Role!);
        }

        return Ok("Registration successful");
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByNameAsync(dto.Email);
        if (user == null || !user.IsActive)
        {
            return Unauthorized("Invalid credentials");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, false);
        if (!result.Succeeded)
        {
            return Unauthorized("Invalid credentials");
        }

        var token = GenerateJwtToken(user);

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

    private string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim("role", user.Role ?? ""),
            new Claim("facilityId", user.FacilityId?.ToString() ?? "")
        };

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