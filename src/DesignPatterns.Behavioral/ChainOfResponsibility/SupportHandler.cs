namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public abstract class SupportHandler(string handlerName) : BaseHandler<SupportTicket, TicketResponse>
{
    protected string HandlerName { get; } = handlerName;
}