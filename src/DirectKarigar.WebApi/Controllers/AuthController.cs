using DirectKarigar.Application.Common.Interfaces;
using DirectKarigar.Application.DTOs;
using DirectKarigar.Domain.Entities;
using DirectKarigar.Domain.Enums;
using DirectKarigar.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DirectKarigar.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtService;

    public AuthController(ApplicationDbContext context, IJwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthUserDto>> Login([FromBody] LoginRequest request)
    {
        var cleanInput = request.EmailOrPhone.Trim().ToLower();
        var user = await _context.Users
            .Include(u => u.CustomerProfile)
            .Include(u => u.ProfessionalProfile)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == cleanInput || u.Phone.Replace(" ", "") == cleanInput.Replace(" ", ""));

        if (user == null)
            return Unauthorized(new { message = "Invalid email/phone or password." });

        var inputHash = DbInitializer.HashPassword(request.Password.Trim());
        if (user.PasswordHash != inputHash)
            return Unauthorized(new { message = "Invalid email/phone or password." });

        var token = _jwtService.GenerateToken(user);
        return Ok(MapToAuthDto(user, token));
    }

    [HttpPost("demo-login")]
    public async Task<ActionResult<AuthUserDto>> DemoLogin([FromBody] DemoLoginRequest request)
    {
        var role = request.Role.Trim().ToLower();
        User? user = role switch
        {
            "customer" => await _context.Users.Include(u => u.CustomerProfile).FirstOrDefaultAsync(u => u.Role == UserRole.Customer),
            "professional" => await _context.Users.Include(u => u.ProfessionalProfile).FirstOrDefaultAsync(u => u.Role == UserRole.Professional),
            "admin" => await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Admin),
            _ => null
        };

        if (user == null)
            return NotFound(new { message = $"Demo account for role '{request.Role}' not found." });

        var token = _jwtService.GenerateToken(user);
        return Ok(MapToAuthDto(user, token));
    }

    private static AuthUserDto MapToAuthDto(User user, string token)
    {
        return new AuthUserDto(
            user.Id,
            user.Name,
            user.Email,
            user.Phone,
            user.Role.ToString().ToLowerInvariant(),
            user.AdminRole?.ToString().ToLowerInvariant(),
            user.ProfessionalProfile?.Id,
            user.CustomerProfile?.Id,
            user.AvatarBg,
            user.Initials,
            user.Locality,
            user.City,
            token
        );
    }
}
