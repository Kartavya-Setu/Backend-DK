using DirectKarigar.Domain.Entities;

namespace DirectKarigar.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
}
