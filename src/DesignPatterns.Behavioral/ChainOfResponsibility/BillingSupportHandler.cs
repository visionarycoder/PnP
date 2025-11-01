namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class BillingSupportHandler() : SupportHandler("Billing Department")
{
    protected override async Task<TicketResponse?> ProcessRequestAsync(SupportTicket ticket)
    {
        await Task.Delay(12);

        if (ticket.Type == TicketType.Billing)
        {
            Console.WriteLine($"[{HandlerName}] 💳 Billing ticket #{ticket.Id} assigned to billing team");
            return TicketResponse.Handle(HandlerName, "Billing Specialist",
                "Billing inquiry routed to billing department", TimeSpan.FromHours(8));
        }

        Console.WriteLine($"[{HandlerName}] Not a billing issue - passing to next handler");
        return null;
    }
}