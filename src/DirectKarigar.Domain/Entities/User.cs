using DirectKarigar.Domain.Common;
using DirectKarigar.Domain.Enums;

namespace DirectKarigar.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public AdminRole? AdminRole { get; set; }
    public string AvatarBg { get; set; } = "#3B82F6";
    public string Initials { get; set; } = string.Empty;
    public string? Locality { get; set; }
    public string? City { get; set; }

    public CustomerProfile? CustomerProfile { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}
