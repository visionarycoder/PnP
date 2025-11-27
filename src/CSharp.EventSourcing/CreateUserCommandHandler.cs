namespace CSharp.EventSourcing;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand>
{
    private readonly IEventSourcedRepository<User> userRepository;

    public CreateUserCommandHandler(IEventSourcedRepository<User> userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task HandleAsync(CreateUserCommand command, CancellationToken token = default)
    {
        var user = new User(command.Name, command.Email);
        await userRepository.SaveAsync(user, token);
    }
}