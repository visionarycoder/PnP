namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class CriticalIssuesHandler() : SupportHandler("Critical Issues Team")
{
    protected override async Task<TicketResponse?> ProcessRequestAsync(SupportTicket ticket)
    {
        await Task.Delay(10);

        if (ticket.Priority == TicketPriority.Critical)
        {
            Console.WriteLine($"[{HandlerName}] 🚨 CRITICAL ticket #{ticket.Id} assigned immediately");
            return TicketResponse.Handle(HandlerName, "Senior Engineer",
                "Critical issue escalated to senior team", TimeSpan.FromHours(1));
        }

        Console.WriteLine($"[{HandlerName}] Not critical priority - passing to next handler");
        return null;
    }
}