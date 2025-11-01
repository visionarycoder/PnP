namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class ProcessingResult<T>
{
    public List<T> Data { get; set; } = [];
    public ProcessingStatus Status { get; set; }
    public Exception? Error { get; set; }
    public ProcessingStatistics Statistics { get; set; } = new();
    public List<string> Logs { get; set; } = [];
}