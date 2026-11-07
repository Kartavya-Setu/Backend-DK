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

    [HttpPost("register-customer")]
    [HttpPost("register/customer")]
    public async Task<ActionResult<AuthUserDto>> RegisterCustomer([FromBody] RegisterCustomerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Name, email, phone, and password are required." });
        }

        var cleanEmail = request.Email.Trim().ToLower();
        var cleanPhone = request.Phone.Trim().Replace(" ", "");

        var exists = await _context.Users.AnyAsync(u =>
            u.Email.ToLower() == cleanEmail ||
            u.Phone.Replace(" ", "") == cleanPhone);

        if (exists)
            return Conflict(new { message = "An account with this email or phone already exists." });

        var initials = ComputeInitials(request.Name);

        var user = new User
        {
            Id = $"user_cust_{Guid.NewGuid():N}",
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            PasswordHash = DbInitializer.HashPassword(request.Password.Trim()),
            Role = UserRole.Customer,
            AvatarBg = "bg-emerald-800",
            Initials = initials,
            Locality = string.IsNullOrWhiteSpace(request.Locality) ? "Indiranagar, Bengaluru" : request.Locality.Trim(),
            City = "Bengaluru",
            CustomerProfile = new CustomerProfile
            {
                Id = $"cust_{Guid.NewGuid():N}",
                City = "Bengaluru",
                Locality = string.IsNullOrWhiteSpace(request.Locality) ? "Indiranagar, Bengaluru" : request.Locality.Trim(),
                Lat = 12.9716,
                Lng = 77.5946,
                InternalProRatingSum = 5.0,
                InternalProRatingCount = 1,
                InternalCancellationRate = 0.0,
                InternalPaymentReliability = 100.0
            }
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return CreatedAtAction(nameof(Login), MapToAuthDto(user, token));
    }

    [HttpPost("register-karigar")]
    [HttpPost("register/karigar")]
    [HttpPost("register-professional")]
    public async Task<ActionResult<AuthUserDto>> RegisterProfessional([FromBody] RegisterProfessionalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Name, phone, and password are required." });
        }

        var email = string.IsNullOrWhiteSpace(request.Email)
            ? $"{request.Name.Trim().ToLowerInvariant().Replace(" ", "")}@karigardirect.in"
            : request.Email.Trim();

        var cleanEmail = email.ToLower();
        var cleanPhone = request.Phone.Trim().Replace(" ", "");

        var exists = await _context.Users.AnyAsync(u =>
            u.Email.ToLower() == cleanEmail ||
            u.Phone.Replace(" ", "") == cleanPhone);

        if (exists)
            return Conflict(new { message = "An account with this email or phone already exists." });

        var initials = ComputeInitials(request.Name);

        var user = new User
        {
            Id = $"user_pro_{Guid.NewGuid():N}",
            Name = request.Name.Trim(),
            Email = email,
            Phone = request.Phone.Trim(),
            PasswordHash = DbInitializer.HashPassword(request.Password.Trim()),
            Role = UserRole.Professional,
            AvatarBg = "bg-amber-600",
            Initials = initials,
            Locality = string.IsNullOrWhiteSpace(request.Locality) ? "Koramangala, Bengaluru" : request.Locality.Trim(),
            City = "Bengaluru",
            ProfessionalProfile = new ProfessionalProfile
            {
                Id = $"pro_{Guid.NewGuid():N}",
                Trade = string.IsNullOrWhiteSpace(request.Trade) ? "Electrician & Wireman" : request.Trade.Trim(),
                ExperienceYears = request.ExperienceYears > 0 ? request.ExperienceYears : 5,
                City = "Bengaluru",
                Locality = string.IsNullOrWhiteSpace(request.Locality) ? "Koramangala, Bengaluru" : request.Locality.Trim(),
                UpiId = string.IsNullOrWhiteSpace(request.UpiId) ? "pro@upi" : request.UpiId.Trim(),
                Verified = false,
                PublicRating = 5.0,
                TotalJobsDone = 0,
                ActiveOrdersCount = 0,
                MaxConcurrentOrders = 2,
                TrustScore = 80
            }
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return CreatedAtAction(nameof(Login), MapToAuthDto(user, token));
    }

    [HttpPost("register-admin")]
    [HttpPost("register/admin")]
    public async Task<ActionResult<AuthUserDto>> RegisterAdmin([FromBody] RegisterAdminRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Phone) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Name, email, phone, and password are required." });
        }

        var cleanEmail = request.Email.Trim().ToLower();
        var cleanPhone = request.Phone.Trim().Replace(" ", "");

        var exists = await _context.Users.AnyAsync(u =>
            u.Email.ToLower() == cleanEmail ||
            u.Phone.Replace(" ", "") == cleanPhone);

        if (exists)
            return Conflict(new { message = "An account with this email or phone already exists." });

        var adminRole = AdminRole.SuperAdmin;
        if (!string.IsNullOrWhiteSpace(request.AdminRole) &&
            Enum.TryParse<AdminRole>(request.AdminRole, true, out var parsedRole))
        {
            adminRole = parsedRole;
        }

        var initials = ComputeInitials(request.Name);

        var user = new User
        {
            Id = $"user_admin_{Guid.NewGuid():N}",
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            PasswordHash = DbInitializer.HashPassword(request.Password.Trim()),
            Role = UserRole.Admin,
            AdminRole = adminRole,
            AvatarBg = "bg-stone-900",
            Initials = initials,
            Locality = string.IsNullOrWhiteSpace(request.Locality) ? "HQ - Indiranagar, Bengaluru" : request.Locality.Trim(),
            City = "Bengaluru"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = _jwtService.GenerateToken(user);
        return CreatedAtAction(nameof(Login), MapToAuthDto(user, token));
    }

    private static string ComputeInitials(string name)
    {
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => "U",
            1 => parts[0].Length >= 2 ? parts[0].Substring(0, 2).ToUpperInvariant() : parts[0].ToUpperInvariant(),
            _ => $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant()
        };
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
