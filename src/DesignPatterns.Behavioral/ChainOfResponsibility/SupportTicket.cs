namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class SupportTicket
{
    public int Id { get; init; }
    public string Title { get; init; } = "";
    public string Description { get; init; } = "";
    public TicketPriority Priority { get; init; }
    public TicketType Type { get; init; }
    public string CustomerEmail { get; init; } = "";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}