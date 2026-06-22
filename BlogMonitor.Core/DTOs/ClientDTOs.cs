using System;

namespace BlogMonitor.Core.DTOs
{
    public record CreateClientRequest(string FullName, string Email, string? CompanyName, string? Phone);
    public record UpdateClientRequest(string FullName, string Email, string? CompanyName, string? Phone, string Status);
    public record ClientResponse(int Id, string FullName, string Email, string? CompanyName, string? Phone, string Status, DateTime CreatedAt, int ActiveUrlCount);
}
