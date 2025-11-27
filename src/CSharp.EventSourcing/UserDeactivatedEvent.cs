namespace CSharp.EventSourcing;

public class UserDeactivatedEvent : DomainEvent
{
    public DateTime DeactivationDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}