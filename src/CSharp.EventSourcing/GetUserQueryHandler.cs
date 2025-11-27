namespace CSharp.EventSourcing;

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, User?>
{
    private readonly IEventSourcedRepository<User> userRepository;

    public GetUserQueryHandler(IEventSourcedRepository<User> userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<User?> HandleAsync(GetUserQuery query, CancellationToken token = default)
    {
        return await userRepository.GetByIdAsync(query.UserId, token);
    }
}