using System;

namespace BlogMonitor.Core.DTOs
{
    public record MonitoringLogResponse(int Id, int? ClientUrlId, string? Url, string EventType, string Message, string Level, DateTime Timestamp);
}
