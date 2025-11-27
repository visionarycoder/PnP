namespace CSharp.EventSourcing;

public class UserEmailChangedEvent : DomainEvent
{
    public string OldEmail { get; set; } = string.Empty;
    public string NewEmail { get; set; } = string.Empty;
}