namespace CSharp.EventSourcing;

public class UserProjection : IEventProjection
{
    private readonly Dictionary<Guid, UserReadModel> users = new();

    public string ProjectionName => "UserProjection";

    public Task ProjectAsync(IEvent domainEvent, CancellationToken token = default)
    {
        switch (domainEvent)
        {
            case UserCreatedEvent created:
                users[created.AggregateId] = new UserReadModel
                {
                    Id = created.AggregateId,
                    Name = created.Name,
                    Email = created.Email,
                    IsActive = true,
                    CreatedDate = created.Timestamp
                };
                break;
            
            case UserEmailChangedEvent emailChanged:
                if (users.TryGetValue(emailChanged.AggregateId, out var user))
                {
                    user.Email = emailChanged.NewEmail;
                }
                break;
                
            case UserDeactivatedEvent deactivated:
                if (users.TryGetValue(deactivated.AggregateId, out var userToDeactivate))
                {
                    userToDeactivate.IsActive = false;
                }
                break;
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken token = default)
    {
        users.Clear();
        return Task.CompletedTask;
    }

    public IEnumerable<UserReadModel> GetAllUsers() => users.Values;
    public UserReadModel? GetUser(Guid id) => users.TryGetValue(id, out var user) ? user : null;
}