namespace AirbnbClone.Api.Application.DTOs.Auth;

public class AuthLoginRequestDTO
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
