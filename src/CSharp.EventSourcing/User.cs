namespace CSharp.EventSourcing;

public class User : AggregateRoot
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedDate { get; private set; }

    public User() { }

    public User(string name, string email) : base()
    {
        RaiseEvent(new UserCreatedEvent { Name = name, Email = email });
    }

    public void ChangeEmail(string newEmail)
    {
        if (Email != newEmail)
        {
            RaiseEvent(new UserEmailChangedEvent { OldEmail = Email, NewEmail = newEmail });
        }
    }

    public void Deactivate(string reason)
    {
        if (IsActive)
        {
            RaiseEvent(new UserDeactivatedEvent 
            { 
                DeactivationDate = DateTime.UtcNow, 
                Reason = reason 
            });
        }
    }

    protected override void RegisterEventHandlers()
    {
        RegisterEventHandler<UserCreatedEvent>(ApplyUserCreatedEvent);
        RegisterEventHandler<UserEmailChangedEvent>(ApplyUserEmailChangedEvent);
        RegisterEventHandler<UserDeactivatedEvent>(ApplyUserDeactivatedEvent);
    }

    private void ApplyUserCreatedEvent(UserCreatedEvent @event)
    {
        Name = @event.Name;
        Email = @event.Email;
        CreatedDate = @event.Timestamp;
        IsActive = true;
    }

    private void ApplyUserEmailChangedEvent(UserEmailChangedEvent @event)
    {
        Email = @event.NewEmail;
    }

    private void ApplyUserDeactivatedEvent(UserDeactivatedEvent @event)
    {
        IsActive = false;
    }
}