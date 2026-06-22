namespace BlogMonitor.Core.Interfaces
{
    public interface ITfidfSummariserService
    {
        string? Summarise(string markdownFilePath);
    }
}
