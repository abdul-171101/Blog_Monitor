using System;

namespace BlogMonitor.Core.DTOs
{
    public record RegisterRequest(string Email, string Password, string Role);
    public record LoginRequest(string Email, string Password);
    public record LoginResponse(string AccessToken, int ExpiresInMinutes, string Role);
    public record RefreshResponse(string AccessToken, int ExpiresInMinutes);
}
