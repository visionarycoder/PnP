namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class TechnicalSupportHandler() : SupportHandler("Technical Support")
{
    protected override async Task<TicketResponse?> ProcessRequestAsync(SupportTicket ticket)
    {
        await Task.Delay(15);

        if (ticket.Type == TicketType.Technical)
        {
            var resolution = ticket.Priority switch
            {
                TicketPriority.High => TimeSpan.FromHours(4),
                TicketPriority.Medium => TimeSpan.FromHours(24),
                TicketPriority.Low => TimeSpan.FromDays(3),
                _ => TimeSpan.FromDays(1)
            };

            Console.WriteLine($"[{HandlerName}] 🔧 Technical ticket #{ticket.Id} assigned to tech support");
            return TicketResponse.Handle(HandlerName, "Tech Support Agent",
                "Technical issue assigned to specialist", resolution);
        }

        Console.WriteLine($"[{HandlerName}] Not a technical issue - passing to next handler");
        return null;
    }
}